using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using totlib;
using Xunit;

namespace tot.Tests
{
    public abstract class DataAccessorTests
    {
        protected abstract IDataAccessor GetConfiguration();

        protected TestClock Clock { get; } = new TestClock();

        [Fact]
        public void When_a_series_is_defined_then_the_file_is_created()
        {
            var dataAccessor = GetConfiguration();

            dataAccessor.CreateSeries("somefile.csv", "hello");

            dataAccessor.ReadCsv("somefile.csv")
                        .Should()
                        .Be("time,hello" + Environment.NewLine);
        }

        [Fact]
        public void When_a_series_is_appended_then_the_file_contains_the_appended_values()
        {
            var dataAccessor = GetConfiguration();

            dataAccessor.CreateSeries("series", "one", "two", "three");

            dataAccessor.AppendValues("series", "1", "2", "3");
            var firstTimeEntry = Clock.Now;
            Clock.AdvanceBy(1.Seconds());
            var secondTimeEntry = Clock.Now;
            dataAccessor.AppendValues("series", "11", "22", "33");

            dataAccessor.ReadCsv("series")
                        .Should()
                        .Be($@"time,one,two,three
{firstTimeEntry:s},1,2,3
{secondTimeEntry:s},11,22,33
".NormalizeLineEndings());
        }

        [Fact]
        public void It_can_list_defined_series()
        {
            var dataAccessor = GetConfiguration();

            dataAccessor.CreateSeries("series1", "one");
            dataAccessor.CreateSeries("series2", "one", "two");
            dataAccessor.CreateSeries("series3", "one", "two", "three");

            dataAccessor.ListSeries()
                        .Should()
                        .BeEquivalentTo("series1",
                                        "series2",
                                        "series3");
        }

        [Fact]
        public void It_returns_an_error_if_attempting_to_an_append_to_an_undefined_series()
        {
            var dataAccessor = GetConfiguration();

            Action append = () =>
                dataAccessor.AppendValues("nope");

            append.Should()
                  .Throw<TotException>()
                  .Which
                  .Message
                  .Should()
                  .Be("Series \"nope\" hasn't been defined. Use tot add to define it.");
        }

        [Fact]
        public void It_returns_an_error_if_too_many_values_are_added_to_a_series()
        {
            var dataAccessor = GetConfiguration();

            dataAccessor.CreateSeries("series", "one", "two");

            Action append = () => dataAccessor.AppendValues("series", "one", "two", "three");

            append.Should()
                  .Throw<TotException>()
                  .Which
                  .Message
                  .Should()
                  .Be("Too many values specified. Series \"series\" expects values: one,two");
        }

        [Fact]
        public void It_returns_an_error_if_too_few_values_are_added_to_a_series()
        {
            var dataAccessor = GetConfiguration();

            dataAccessor.CreateSeries("series", "one", "two");

            Action append = () => dataAccessor.AppendValues("series", "one");

            append.Should()
                  .Throw<TotException>()
                  .Which
                  .Message
                  .Should()
                  .Be("Too few values specified. Series \"series\" expects values: one,two");
        }

        [Fact]
        public void Reading_a_nonexistent_series_throws()
        {
            Action read = () => GetConfiguration().ReadCsv("nonexistent.csv");

            read
                .Should()
                .Throw<TotException>()
                .Which
                .Message
                .Should()
                .Be("Series \"nonexistent.csv\" hasn't been defined. Use tot add to define it.");
        }

        [Fact]
        public void A_series_can_be_created_with_no_columns()
        {
            var configuration = GetConfiguration();

            configuration.CreateSeries("things");

            configuration.ReadCsv("things").Should().Be("time" + Environment.NewLine);
        }

        [Fact]
        public void Empty_values_can_be_appended_to_record_just_a_timestamp()
        {
            var configuration = GetConfiguration();

            configuration.CreateSeries("things");

            configuration.AppendValues("things");

            configuration.ReadCsv("things").Should().Be($@"time
{Clock.Now:s}
".NormalizeLineEndings());
        }

        [Fact]
        public void Values_cannot_contain_commas()
        {
            var configuration = GetConfiguration();

            configuration.CreateSeries("stuff", "value");

          Action append =()=>  configuration.AppendValues("stuff", "one,two");

          append.Should()
                .Throw<TotException>()
                .Which
                .Message
                .Should()
                .Be("Values cannot contain commas but: \"one,two\"");
        }

    }

    public class FileDataAccessorTests : DataAccessorTests, IDisposable
    {
        private readonly Lazy<(FileBasedDataAccessor configuration, DisposableDirectory tempDir)> _directory;

        public FileDataAccessorTests()
        {
            _directory = new Lazy<(FileBasedDataAccessor, DisposableDirectory)>(() =>
            {
                var tempDir = DisposableDirectory.Create();

                return (new FileBasedDataAccessor(tempDir.Directory, Clock), tempDir);
            });
        }

        protected override IDataAccessor GetConfiguration()
        {
            return _directory.Value.configuration;
        }

        public void Dispose()
        {
            if (_directory.IsValueCreated)
            {
                _directory.Value.tempDir.Dispose();
            }
        }
    }

    public class InMemoryDataAccessorTests : DataAccessorTests
    {
        protected override IDataAccessor GetConfiguration()
        {
            return new InMemoryDataAccessor(Clock);
        }
    }
}
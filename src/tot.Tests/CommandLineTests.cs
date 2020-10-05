using System;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using FluentAssertions;
using FluentAssertions.Extensions;
using totlib;
using Xunit;
using static System.Environment;

namespace tot.Tests
{
    public class CommandLineTests
    {
        private readonly IDataAccessor dataAccessor;
        private readonly Parser _parser;
        private readonly TestClock _clock;
        private readonly TestConsole _console;

        public CommandLineTests()
        {
            _clock = new TestClock();
            dataAccessor = new InMemoryDataAccessor(_clock);
            _console = new TestConsole();
            _parser = CommandLineParser.Create(dataAccessor);
        }

        [Fact]
        public void When_a_series_is_added_it_creates_a_file_containing_the_column_headers()
        {
            _parser.Invoke("add ate what howMany deliciousness");

            dataAccessor.ReadCsv("ate.csv")
                        .Should()
                        .Be("time,what,howMany,deliciousness" + NewLine);
        }

        [Fact]
        public void It_can_add_records_to_a_series()
        {
            _parser.Invoke("add ate what howMany");

            _parser.Invoke("ate bananas 3");
            _parser.Invoke("ate bananas 5");

            var csv = dataAccessor.ReadCsv("ate.csv");

            csv.Should()
               .Be($@"time,what,howMany
{_clock.Now:s},bananas,3
{_clock.Now:s},bananas,5
".NormalizeLineEndings());
        }

        [Fact]
        public void Time_can_be_specified_as_a_date_string_when_adding_records_to_a_series()
        {
            _parser.Invoke("add ate what howMany");

            var time1 = DateTime.Now;
            var time2 = DateTime.Now.AddMinutes(4);
            var time3 = DateTime.Now.AddMinutes(4);

            _parser.Invoke($"--time {time1:s} ate bananas 3");
            _parser.Invoke($"ate --time {time2:s} bananas 5");
            _parser.Invoke($"ate bananas 8 --time {time3:s} ");

            var csv = dataAccessor.ReadCsv("ate.csv");

            csv.Should()
               .Be($@"time,what,howMany
{time1:s},bananas,3
{time2:s},bananas,5
{time3:s},bananas,8
".NormalizeLineEndings());
        }
        
        [Fact]
        public void Time_can_be_specified_as_a_duration_when_adding_records_to_a_series()
        {
            _parser.Invoke("add ate what howMany");

            var time1 = _clock.Now.AddHours(-8);
            var time2 = _clock.Now.Subtract(4.Minutes());
            var time3 = _clock.Now.AddMinutes(5).AddSeconds(23);

            _parser.Invoke($"--time -8h ate bananas 3");
            _parser.Invoke($"ate --time -4m bananas 5");
            _parser.Invoke($"ate bananas 8 --time 5m23s");

            var csv = dataAccessor.ReadCsv("ate.csv");

            csv.Should()
               .Be($@"time,what,howMany
{time1:s},bananas,3
{time2:s},bananas,5
{time3:s},bananas,8
".NormalizeLineEndings());
        }

        [Theory]
        [InlineData("2040-08-31T10:03:47", "8pm", "2040-08-30T20:00:00")]
        [InlineData("2040-08-31T14:03:47", "7am", "2040-08-31T07:00:00")]
        public void When_time_is_specified_without_a_date_then_it_chooses_a_time_in_the_past(
            string currentTime,
            string timeArgument,
            string expected)
        {
            _clock.AdvanceTo(DateTime.Parse(currentTime));

            _parser.Invoke("add x");

            _parser.Invoke($"x -t {timeArgument}");

            var csv = dataAccessor.ReadCsv("x.csv");

            csv.Should()
               .Be($@"time
{expected}
".NormalizeLineEndings());
        }

        [Fact]
        public void It_returns_an_error_if_a_series_is_added_twice()
        {
            _parser.Invoke("add ate what");

            var result = _parser.Invoke("add ate what");

            result.Should().NotBe(0);
        }

        [Fact]
        public void It_can_list_defined_series()
        {
            _parser.Invoke("add fruit name deliciousness");
            _parser.Invoke("add animal furriness cuteness");

            _parser.Invoke("list", _console);

            _console.Out
                    .ToString()
                    .Split(NewLine)
                    .Should()
                    .ContainInOrder("animal", "fruit");
        }

        [Fact]
        public void It_is_not_necessary_to_specify_columns()
        {
            var result = _parser.Invoke("add cat");

            result.Should().Be(0);
        }

        [Fact]
        public void It_can_provide_completions_on_known_measures()
        {
            _parser.Invoke("add apple");
            _parser.Invoke("add banana");
            _parser.Invoke("add cherry");

            _parser.Parse("")
                   .GetSuggestions()
                   .Should()
                   .Contain(new[] { "apple", "banana", "cherry" });
        }

        [Fact]
        public void When_series_is_already_defined_then_a_friendly_error_is_displayed()
        {
            _parser.Invoke("add something", _console);

            var testConsole = new TestConsole();
            var result = _parser.Invoke("add something", testConsole);

            result.Should().Be(1);

            testConsole.Error.ToString().Should().Be("Series \"something\" has already been defined." + NewLine);
        }

        [Fact]
        public void When_series_is_not_defined_then_a_friendly_error_is_displayed()
        {
            var result = _parser.Invoke("something", _console);

            result.Should().Be(1);

            _console.Error.ToString().Should().Be("Series \"something\" hasn't been defined. Use tot add to define it." + NewLine);
        }

        [Fact]
        public void When_too_many_values_are_appended_then_a_friendly_error_is_displayed()
        {
            _parser.Invoke("add series one two");

            var console = new TestConsole();
            var result = _parser.Invoke("series 1 2 3", console);

            result.Should().Be(1);

            console.Error.ToString().Should().Be("Too many values specified. Series \"series\" expects values for: one,two" + NewLine);
        }

        [Fact]
        public void When_too_few_values_are_appended_then_a_friendly_error_is_displayed()
        {
            _parser.Invoke("add series one two");

            var console = new TestConsole();
            var result = _parser.Invoke("series 1", console);

            result.Should().Be(1);

            console.Error.ToString().Should().Be("Too few values specified. Series \"series\" expects values for: one,two" + NewLine);
        }
    }
}
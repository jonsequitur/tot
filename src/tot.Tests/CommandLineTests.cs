using System;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using FluentAssertions;
using totlib;
using Xunit;
using Xunit.Abstractions;

namespace tot.Tests
{
    public class CommandLineTests
    {
        private readonly IDataAccessor dataAccessor;
        private readonly Parser _parser;
        private readonly TestClock _clock;
        private readonly TestConsole _console;
        private ITestOutputHelper _output;

        public CommandLineTests(ITestOutputHelper output)
        {
            _output = output;
            _clock = new TestClock();
            dataAccessor = new InMemoryDataAccessor(_clock);
            _console = new TestConsole();
            _parser = Program.CreateCommandLineParser(dataAccessor);
        }

        [Fact]
        public void When_a_series_is_added_it_creates_a_file_containing_the_column_headers()
        {
            _parser.Invoke("add ate what howMany deliciousness");

            dataAccessor.ReadCsv("ate.csv")
                        .Should()
                        .Be("time,what,howMany,deliciousness" + Environment.NewLine);
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
");
        }

        [Fact]
        public void Time_can_be_specified_when_adding_records_to_a_series()
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
");
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
                    .Split(Environment.NewLine)
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
    }
}
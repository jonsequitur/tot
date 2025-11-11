using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using FluentAssertions.Extensions;
using totlib;
using Xunit;
using static System.Environment;

namespace tot.Tests;

public class CommandLineTests
{
    private readonly IDataAccessor dataAccessor;
    private readonly RootCommand _rootCommand;
    private readonly TestClock _clock;
    private readonly StringWriter _outputWriter;
    private readonly StringWriter _errorWriter;

    public CommandLineTests()
    {
        _clock = new TestClock();
        dataAccessor = new InMemoryDataAccessor(_clock);
        _outputWriter = new StringWriter();
        _errorWriter = new StringWriter();
        _rootCommand = CommandLineParser.Create(dataAccessor);
    }

    private int Invoke(string commandLine)
    {
        // Reset the output writers
        _outputWriter.GetStringBuilder().Clear();
        _errorWriter.GetStringBuilder().Clear();
        
        // Capture console output
        var originalOut = Console.Out;
        var originalError = Console.Error;
        
        try
        {
            Console.SetOut(_outputWriter);
            Console.SetError(_errorWriter);
            
            var args = ParseCommandLine(commandLine);
            return _rootCommand.Parse(args).Invoke();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
    
    private string[] ParseCommandLine(string commandLine)
    {
        // Simple command line parsing - split on spaces but respect quotes
        var args = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            
            if (c == '"' && (i == 0 || commandLine[i-1] != '\\'))
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }
        
        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }
        
        return args.ToArray();
    }

    public string Output => _outputWriter.ToString();
    public string Error => _errorWriter.ToString();

    [Fact]
    public void When_a_series_is_added_it_creates_a_file_containing_the_column_headers()
    {
        Invoke("add ate what howMany deliciousness");

        dataAccessor.ReadLines("ate.csv")
                    .Should()
                    .BeEquivalentTo("time,what,howMany,deliciousness");
    }

    [Fact]
    public void It_can_add_records_to_a_series()
    {
        Invoke("add ate what howMany");

        Invoke("ate bananas 3");
        Invoke("ate bananas 5");

        var lines = dataAccessor.ReadLines("ate.csv");

        lines.Should()
             .BeEquivalentTo(
                 "time,what,howMany",
                 $"{_clock.Now:s},bananas,3",
                 $"{_clock.Now:s},bananas,5");
    }

    [Fact]
    public void Time_can_be_specified_as_a_date_string_when_adding_records_to_a_series()
    {
        Invoke("add ate what howMany");

        var time1 = DateTime.Now;
        var time2 = DateTime.Now.AddMinutes(4);
        var time3 = DateTime.Now.AddMinutes(4);

        Invoke($"--time {time1:s} ate bananas 3");
        Invoke($"ate --time {time2:s} bananas 5");
        Invoke($"ate bananas 8 --time {time3:s} ");

        var lines = dataAccessor.ReadLines("ate.csv");

        lines.Should()
             .BeEquivalentTo(
                 "time,what,howMany",
                 $"{time1:s},bananas,3",
                 $"{time2:s},bananas,5",
                 $"{time3:s},bananas,8");
    }

    [Fact]
    public void Time_can_be_specified_as_a_duration_when_adding_records_to_a_series()
    {
        Invoke("add ate what howMany");

        var time1 = _clock.Now.AddHours(-8);
        var time2 = _clock.Now.Subtract(4.Minutes());
        var time3 = _clock.Now.AddMinutes(5).AddSeconds(23);

        Invoke("--time -8h ate bananas 3");
        Invoke("ate --time -4m bananas 5");
        Invoke("ate bananas 8 --time 5m23s");

        var lines = dataAccessor.ReadLines("ate.csv");

        lines.Should()
             .BeEquivalentTo(
                 "time,what,howMany",
                 $"{time1:s},bananas,3",
                 $"{time2:s},bananas,5",
                 $"{time3:s},bananas,8");
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

        Invoke("add x");

        Invoke($"x -t {timeArgument}");

        var lines = dataAccessor.ReadLines("x.csv");

        lines.Should()
             .BeEquivalentTo(
                 "time",
                 expected);
    }

    [Fact]
    public void It_returns_an_error_if_a_series_is_added_twice()
    {
        Invoke("add ate what");

        var result = Invoke("add ate what");

        result.Should().NotBe(0);
    }

    [Fact]
    public void It_is_not_necessary_to_specify_columns()
    {
        var result = Invoke("add cat");

        result.Should().Be(0);
    }

    [Fact]
    public void tot_can_provide_completions_on_known_series()
    {
        Invoke("add apple");
        Invoke("add banana");
        Invoke("add cherry");

        _rootCommand.Parse("")
                   .GetCompletions()
                   .Select(c => c.Label)
                   .Should()
                   .Contain(new[] { "apple", "banana", "cherry" });
    }

    [Fact]
    public void tot_list_can_provide_completions_on_known_series()
    {
        Invoke("add apple");
        Invoke("add banana");
        Invoke("add cherry");

        _rootCommand.Parse("list ")
                   .GetCompletions()
                   .Select(c => c.Label)
                   .Should()
                   .Contain(new[] { "apple", "banana", "cherry" });
    }

    [Fact]
    public void When_series_is_already_defined_then_a_friendly_error_is_displayed()
    {
        Invoke("add something");

        var result = Invoke("add something");

        result.Should().Be(1);

        Error.Should().Be("Series \"something\" has already been defined." + NewLine);
    }

    [Fact]
    public void When_series_is_not_defined_then_a_friendly_error_is_displayed()
    {
        var result = Invoke("something");

        result.Should().Be(1);

        Error.ToString().Should().Be("Series \"something\" hasn't been defined. Use tot add to define it." + NewLine);
    }

    [Fact]
    public void When_too_many_values_are_appended_then_a_friendly_error_is_displayed()
    {
        Invoke("add series one two");

        var result = Invoke("series 1 2 3");

        result.Should().Be(1);

        Error.Should().Be("Too many values specified. Series \"series\" expects values for: one,two" + NewLine);
    }

    [Fact]
    public void When_too_few_values_are_appended_then_a_friendly_error_is_displayed()
    {
        Invoke("add series one two");

        var result = Invoke("series 1");

        result.Should().Be(1);

        Error.Should().Be("Too few values specified. Series \"series\" expects values for: one,two" + NewLine);
    }

    [Fact]
    public void It_can_list_defined_series()
    {
        Invoke("add fruit name deliciousness");
        Invoke("add animal furriness cuteness");

        Invoke("list");

        Output
                .ToString()
                .Split(NewLine)
                .Should()
                .BeEquivalentTo("animal", "fruit");
    }

    [Fact]
    public void It_can_list_the_contents_of_a_series()
    {
        Invoke("add fruit name deliciousness");
        Invoke("fruit apple 3");
        Invoke("fruit banana 19");
        Invoke("fruit cherry 2000");

        Invoke("list fruit");

        Output
                .ToString()
                .Split(NewLine)
                .Should()
                .BeEquivalentTo(
                    $"{_clock.Now:s},apple,3",
                    $"{_clock.Now:s},banana,19",
                    $"{_clock.Now:s},cherry,2000",
                    "");
    }

    [Fact]
    public void Listed_series_items_are_returned_in_chronological_order_when_not_filtered()
    {
        Invoke("add things");
        Invoke("things -t \"2020-10-06\"");
        Invoke("things -t \"2020-10-04 3pm\"");
        Invoke("things -t \"2020-10-04 1pm\"");

        Invoke("list things");

        Output
                .ToString()
                .Split(NewLine)
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        "2020-10-04T13:00:00",
                        "2020-10-04T15:00:00",
                        "2020-10-06T00:00:00",
                        ""
                    },
                    config: c => c.WithStrictOrdering());
    }


    [Fact]
    public void Listed_series_items_are_returned_in_chronological_order_when_filtered()
    {
        Invoke("add things");
        Invoke("things -t \"2020-10-06\"");
        Invoke("things -t \"2020-10-04 3pm\"");
        Invoke("things -t \"2020-10-04 1pm\"");

        Invoke("list things --after \"2020-10-04\"");

        Output
                .ToString()
                .Split(NewLine)
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        "2020-10-04T13:00:00",
                        "2020-10-04T15:00:00",
                        ""
                    },
                    config: c => c.WithStrictOrdering());
    }

    [Fact]
    public void Listed_series_contents_can_be_filtered_to_a_specific_day()
    {
        Invoke("add fruit name deliciousness");
        Invoke("fruit apple 3 -t \"2020-10-04 3pm\"");
        Invoke("fruit banana 19 -t \"2020-10-05 3pm\"");
        Invoke("fruit cherry 2000 -t \"2020-10-06\"");
        Invoke("fruit durian 89 -t \"2020-10-06 3pm\"");

        Invoke("list fruit --after \"2020-10-05\"");

        Output
                .ToString()
                .Split(NewLine)
                .Should()
                .BeEquivalentTo(
                    "2020-10-05T15:00:00,banana,19",
                    "");
    }

    [Fact]
    public void Listed_series_contents_can_be_filtered_to_after_a_specified_time()
    {
        _clock.AdvanceTo(DateTime.Parse("2020-10-05"));

        Invoke("add things");
        Invoke("things -t \"2020-10-03\"");
        Invoke("things -t \"2020-10-04 3pm\"");
        Invoke("things -t \"2020-10-04 1pm\"");

        Invoke("list things --after -1d");

        Output
                .ToString()
                .Split(NewLine)
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        "2020-10-04T13:00:00",
                        "2020-10-04T15:00:00",
                        ""
                    },
                    config: c => c.WithStrictOrdering());
    }

    [Fact]
    public void Unique_days_on_which_series_entries_occurred_can_be_listed()
    {
        _clock.AdvanceTo(DateTime.Parse("2020-10-05"));

        Invoke("add things one two");
        Invoke("things -t \"2020-10-03 12:31am\" 1 2");
        Invoke("things -t \"2020-10-04 3pm\" 3 4");
        Invoke("things -t \"2020-10-04 1pm\" 5 6");

        Invoke("list things --days");

        Output
                .ToString()
                .Split(NewLine)
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        "2020-10-03T00:00:00",
                        "2020-10-04T00:00:00",
                        ""
                    },
                    config: c => c.WithStrictOrdering());
    }

    [Fact]
    public void Latest_shows_the_most_recent_entries_in_each_series()
    {
        Invoke("add one");
        Invoke("one -t 2020-10-25");
        Invoke("one -t 2020-10-26");
        Invoke("one -t 2020-10-24");
        Invoke("one -t 2020-10-23");

        Invoke("add two col1 col2");
        Invoke("two a b -t 2020-10-08");
        Invoke("two c d -t 2020-10-08");

        Invoke("latest");

        Output
                .ToString()
                .Split(NewLine)
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        "two:",
                        "    2020-10-08T00:00:00,c,d",
                        "one:",
                        "    2020-10-26T00:00:00",
                        ""
                    },
                    config: c => c.WithStrictOrdering());
    }
}






﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.CommandLine.Tests.Utility;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static System.Environment;

namespace System.CommandLine.Tests
{
    public class UseHelpTests
    {
        private readonly TestConsole _console = new();

        [Fact]
        public async Task UseHelp_writes_help_for_the_specified_command()
        {
            var command = new Command("command");
            var subcommand = new Command("subcommand");
            command.AddCommand(subcommand);

            var parser =
                new CommandLineBuilder(new RootCommand
                    {
                        command
                    })
                    .UseHelp()
                    .Build();

            var result = parser.Parse("command subcommand --help");

            await result.InvokeAsync(_console);

            _console.Out.ToString().Should().Contain($"{RootCommand.ExecutableName} command subcommand [options]");
        }
         
        [Fact]
        public async Task UseHelp_interrupts_execution_of_the_specified_command()
        {
            var wasCalled = false;
            var command = new Command("command");
            var subcommand = new Command("subcommand");
            subcommand.Handler = CommandHandler.Create(() => wasCalled = true);
            command.AddCommand(subcommand);

            var parser =
                new CommandLineBuilder(new RootCommand
                    {
                        command
                    })
                    .UseHelp()
                    .Build();

            await parser.InvokeAsync("command subcommand --help", _console);

            wasCalled.Should().BeFalse();
        }

        [Theory]
        [InlineData("-h")]
        [InlineData("--help")]
        [InlineData("-?")]
        [InlineData("/?")]
        public async Task UseHelp_accepts_default_values(string value)
        {
            var parser =
                new CommandLineBuilder(new RootCommand
                    {
                        new Command("command")
                    })
                    .UseHelp()
                    .Build();

            await parser.InvokeAsync($"command {value}", _console);

            _console.Should().ShowHelp();
        }

        [Fact]
        public async Task UseHelp_does_not_display_when_option_defined_with_same_alias()
        {
            var command = new Command("command");
            command.AddOption(new Option("-h"));
            
            var parser =
                new CommandLineBuilder(new RootCommand
                    {
                        command
                    })
                    .UseHelp()
                    .Build();

            var result = parser.Parse("command -h");

            await result.InvokeAsync(_console);

            _console.Out.ToString().Should().BeEmpty();
        }

        [Fact]
        public void There_are_no_parse_errors_when_help_is_invoked_on_root_command()
        {
            var parser = new CommandLineBuilder()
                .UseHelp()
                .Build();

            var result = parser.Parse("-h");

            result.Errors
                  .Should()
                  .BeEmpty();
        }
        
        [Fact]
        public void There_are_no_parse_errors_when_help_is_invoked_on_subcommand()
        {
            var root = new RootCommand
            {
                new Command("subcommand")
            };

            var parser = new CommandLineBuilder(root)
                         .UseHelp()
                         .Build();

            var result = parser.Parse("subcommand -h");

            result.Errors
                  .Should()
                  .BeEmpty();
        }

        [Fact]
        public void There_are_no_parse_errors_when_help_is_invoked_on_a_command_with_subcommands()
        {
            var root = new RootCommand
            {
                new Command("subcommand")
            };

            var parser = new CommandLineBuilder(root)
                         .UseHelp()
                         .Build();

            var result = parser.Parse("-h");

            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void There_are_no_parse_errors_when_help_is_invoked_on_a_command_with_required_options()
        {
            var command = new RootCommand
            {
                new Option<string>("-x")
                {
                    IsRequired = true
                },
            };

            var result = new CommandLineBuilder(command)
                         .UseHelp()
                         .Build()
                         .Parse("-h");

            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("-h")]
        [InlineData("inner -h")]
        public void UseHelp_can_be_called_more_than_once_on_the_same_CommandLineBuilder(string commandline)
        {
            var root = new RootCommand
            {
                new Command("inner")
            };

            var parser = new CommandLineBuilder(root)
                         .UseHelp()
                         .UseHelp()
                         .Build();

            parser.Invoke(commandline, _console);

            _console.Should().ShowHelp();
        }

        [Theory]
        [InlineData("-h")]
        [InlineData("inner -h")]
        public void UseHelp_can_be_called_more_than_once_on_the_same_command_with_different_CommandLineBuilders(string commandline)
        {
            var root = new RootCommand
            {
                new Command("inner")
            };

            var parser = new CommandLineBuilder(root)
                         .UseHelp()
                         .Build();

            var console1 = new TestConsole();

            parser.Invoke(commandline, console1);

            console1.Should().ShowHelp();

            var parser2 = new CommandLineBuilder(root)
                          .UseHelp()
                          .Build();
            var console2 = new TestConsole();

            parser2.Invoke(commandline, console2);

            console2.Should().ShowHelp();
        }

        [Theory]
        [InlineData("/lost")]
        [InlineData("--confused")]
        public async Task UseHelp_with_custom_aliases_uses_aliases(string helpAlias)
        {
            var parser =
                new CommandLineBuilder()
                    .UseHelp("/lost", "--confused")
                    .Build();

            await parser.InvokeAsync(helpAlias, _console);

            _console.Should().ShowHelp();
        }

        [Theory]
        [InlineData("-h")]
        [InlineData("/h")]
        [InlineData("--help")]
        [InlineData("-?")]
        [InlineData("/?")]
        public async Task UseHelp_with_custom_aliases_default_aliases_replaced(string helpAlias)
        {
            var parser =
                new CommandLineBuilder()
                    .UseHelp("--confused")
                    .Build();

            await parser.InvokeAsync(helpAlias, _console);

            _console.Out.ToString().Should().Be("");
        }


        [Fact]
        public void Help_sections_can_be_replaced()
        {
            var parser = new CommandLineBuilder()
                         .UseHelp(CustomLayout())
                         .Build();

            var console = new TestConsole();
            parser.Invoke("-h", console);

            console.Out.ToString().Should().Be($"one{NewLine}{NewLine}two{NewLine}{NewLine}three{NewLine}{NewLine}{NewLine}");

            IEnumerable<HelpDelegate> CustomLayout()
            {
                yield return ctx => ctx.Output.WriteLine("one");
                yield return ctx => ctx.Output.WriteLine("two");
                yield return ctx => ctx.Output.WriteLine("three");
            }
        }

        [Fact]
        public void Help_sections_can_be_supplemented()
        {
            var command = new RootCommand("hello");
            var parser = new CommandLineBuilder(command)
                         .UseHelp(CustomLayout())
                         .Build();

            var console = new TestConsole();
            parser.Invoke("-h", console);

            var output = console.Out.ToString();
            var defaultHelp = GetDefaultHelp(command);

            var expected = $"first{NewLine}{NewLine}{defaultHelp}last{NewLine}{NewLine}";

            output.Should().Be(expected);

            IEnumerable<HelpDelegate> CustomLayout()
            {
                yield return ctx => ctx.Output.WriteLine("first");

                foreach (var section in HelpBuilder.DefaultLayout())
                {
                    yield return section;
                }

                yield return ctx => ctx.Output.WriteLine("last");
            }
        }
        
        private string GetDefaultHelp(Command command)
        {
            var console = new TestConsole();

            var parser = new CommandLineBuilder(command)
                         .UseHelp()
                         .Build();

            parser.Invoke("-h", console);

            var output = console.Out.ToString();
            output = output.Substring(0, output.Length - NewLine.Length);
            return output;
        }
    }
}

using System;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.CommandLineUtils;
using Lapis.CommandLineUtils.ResultHandlers;

namespace CommandLineSampleApp
{
    [Command("math", Description = "Math operation.")]
    public class MathCommands
    {
        [Command("add", Description = "Add two integers.")]
        public int Add([Argument("a", "An integer.")] int a, 
            [Argument("b", "Another integer.")] int b)
        {
            return a + b;
        }

        [Command]
        public int Subtract([Argument] int a, [Argument] int b)
        {
            return a - b;
        }

        [Command]
        public double Round([Argument] double value, [Argument] int digit = 0)
        {
            return Math.Round(value, digit);
        }

        [Command]
        public double Log([Argument] double value, 
            [Option("-b|--base <base>", "Base, default E.", CommandOptionType.SingleValue)] double b = Math.E)
        {
            return Math.Log(value, b);
        }
    }
}
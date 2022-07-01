using BenchmarkDotNet.Running;

namespace Firely.Sdk.Benchmarks
{
    public class Program
    {
        private static void Main(string[] args) => BenchmarkRunner.Run<ValidatorBenchmarks>();
        //   BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

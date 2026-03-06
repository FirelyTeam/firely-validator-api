using BenchmarkDotNet.Running;

namespace Firely.Sdk.Benchmarks
{
    public class Program
    {
        private static void Main(string[] args) => BenchmarkRunner.Run<ValidatorBenchmarks>();
        //private static void Main(string[] args) => BenchmarkRunner.Run<ResolverBenchmarks>();
        //   BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

using BenchmarkDotNet.Running;

namespace Firely.Sdk.Benchmarks
{
    public class Program
    {
        private static void Main(string[] args)
#if DEBUG
            => BenchmarkRunner.Run<ValidatorBenchmarks>(new BenchmarkDotNet.Configs.DebugInProcessConfig());
#else
            => BenchmarkRunner.Run<ValidatorBenchmarks>();
#endif
        //   BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

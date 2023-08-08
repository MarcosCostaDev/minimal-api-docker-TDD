using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace RinhaBackEnd.Test.Benchmarks;

[Trait("Integration", "Benchmark")]
public class ApiBenchmarkTest : IClassFixture<DockerFixture>, IDisposable
{
    private readonly DockerFixture _dockerFixture;
    private ITestOutputHelper _output;

    public ApiBenchmarkTest(DockerFixture dockerFixture, ITestOutputHelper output)
    {
        _dockerFixture = dockerFixture;
        _output = output;
        dockerFixture.InitOnce(() => new DockerFixtureOptions
        {
            DockerComposeFiles = new[] { "docker-compose.yml", "docker-compose.testing.yml" },
            CustomUpTest = output => output.Any(l => l.Contains("ready for start up") || l.Contains("Attaching to api01, api02, cache, database, proxy")),
            StartupTimeoutSecs = 240
        });
    }

    [Fact]
    public void ApiBenchmark()
    {
        var logger = new AccumulationLogger();

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddLogger(logger)
            .AddValidator(JitOptimizationsValidator.DontFailOnError)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        BenchmarkRunner.Run<PeopleControllerBenchmarkTest>(config);

        // write benchmark summary
        _output.WriteLine(logger.GetLog());
    }

    public void Dispose()
    {
        _dockerFixture.Dispose();
    }
}

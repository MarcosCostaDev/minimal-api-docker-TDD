﻿using RinhaBackEnd.Test.Controllers;

namespace RinhaBackEnd.Test.Benchmarks;

[Trait("ignore", "ignore")]
[SimpleJob(RunStrategy.Monitoring, iterationCount: 10, id: nameof(PeopleControllerBenchmarkTest))]
[RankColumn, MinColumn, MaxColumn, Q1Column, Q3Column, AllStatisticsColumn]
public class PeopleControllerBenchmarkTest : PeopleControllerTest
{
    public PeopleControllerBenchmarkTest()
    {
        _fixture = new ContainerFixture();
    }

    [GlobalSetup]
    public void Setup()
    {
        Thread.Sleep(TimeSpan.FromSeconds(10));
    }

    [IterationCleanup]
    public void Cleanup()
    {
        Thread.Sleep(TimeSpan.FromSeconds(1));
        base.CleanDatabaseAsync().ConfigureAwait(false).GetAwaiter();
    }
}
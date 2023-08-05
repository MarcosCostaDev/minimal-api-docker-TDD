namespace RinhaBackEnd.Test;


[CollectionDefinition("API", DisableParallelization = true)]
public class StartupFixtureCollection : ICollectionFixture<ProgramFixture>
{
}

[CollectionDefinition("Benchmark", DisableParallelization = true)]
public class ContainerFixtureCollection : ICollectionFixture<ContainerFixture>
{
}
namespace RinhaBackEnd.Test;

[CollectionDefinition("docker", DisableParallelization = true)]
public class ContainerFixtureCollection : ICollectionFixture<ContainerFixture>
{
}
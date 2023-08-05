using RinhaBackEnd.Test.Benchmarks;

[assembly: TestFramework("RinhaBackEnd.Test.SkipClasses", nameof(RinhaBackEnd.Test))]
namespace RinhaBackEnd.Test;

public class SkipClasses : XunitTestFramework
{
    public SkipClasses(IMessageSink messageSink)
        : base(messageSink)
    {
    }

    protected override ITestFrameworkDiscoverer CreateDiscoverer(
        IAssemblyInfo assemblyInfo)
        => new MyTestFrameworkDiscoverer(
            assemblyInfo,
            SourceInformationProvider,
            DiagnosticMessageSink);
}

public class MyTestFrameworkDiscoverer : XunitTestFrameworkDiscoverer
{
    public MyTestFrameworkDiscoverer(
        IAssemblyInfo assemblyInfo,
        ISourceInformationProvider sourceProvider,
        IMessageSink diagnosticMessageSink,
        IXunitTestCollectionFactory collectionFactory = null)
        : base(
            assemblyInfo,
            sourceProvider,
            diagnosticMessageSink,
            collectionFactory)
    {
    }

    protected override bool FindTestsForType(ITestClass testClass, bool includeSourceInformation, IMessageBus messageBus, ITestFrameworkDiscoveryOptions discoveryOptions)
    {
        return base.FindTestsForType(testClass, includeSourceInformation, messageBus, discoveryOptions);
    }

    protected override bool IsValidTestClass(ITypeInfo type)
        => base.IsValidTestClass(type) &&
           FilterType(type);

    protected virtual bool FilterType(ITypeInfo type)
    {
        return (type.Name != typeof(PeopleControllerBenchmarkTest).Name);
    }
}

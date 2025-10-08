namespace SimpleObjectDb;

internal record TestObjectA(int Id, string Text, List<TestSubObjectA> SubObjects);

internal record TestSubObjectA(int Id, string Text);

internal record TestObjectB(Guid Id, TestSubObjectB[] Values);

internal record TestSubObjectB(string A, string B, string C, string D, string E);

internal class TestObjectWithEncapsulatedData
{
    public Guid Id { get; private set; }
    public string PublicProperty { get; private set; } = string.Empty;
    private string PrivateProperty { get; set; } = string.Empty;
    public string PropertyWithoutASetter => PrivateProperty;

    public string _publicField = string.Empty;
    private string _privateField = string.Empty;

    public TestObjectWithEncapsulatedData(Guid id, string publicText)
    {
        Id = id;
        PublicProperty = publicText;
        _publicField = publicText;
    }

    public void SetPrivateText(string text)
    {
        PrivateProperty = text;
        _privateField = text;
    }
}

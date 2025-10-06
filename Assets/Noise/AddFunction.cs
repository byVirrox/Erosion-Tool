public class AddFunction : INoiseFunction
{
    private readonly INoiseFunction functionA;
    private readonly INoiseFunction functionB;

    public AddFunction(INoiseFunction a, INoiseFunction b)
    {
        this.functionA = a;
        this.functionB = b;
    }

    public float GetValue(float x, float y)
    {
        return functionA.GetValue(x, y) + functionB.GetValue(x, y);
    }
}

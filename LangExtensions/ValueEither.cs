namespace LangExtensions;

/// <summary>
/// Either monad
/// </summary>
/// <typeparam name="TLeft">Left type</typeparam>
/// <typeparam name="TRight">Right type</typeparam>
public struct ValueEither<TLeft,TRight>
{
    public TLeft? Left { get; }
    public TRight? Right { get; }

    public ValueEither(TLeft left)
    {
        Left = left;
        Right = default;
    }
    
    public ValueEither(TRight right)
    {
        Right = right;
        Left = default;
    }
    
}

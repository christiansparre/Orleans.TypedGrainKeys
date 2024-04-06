namespace Orleans;

public interface ITypedGrainKey<TSelf> where TSelf : ITypedGrainKey<TSelf>
{
    static abstract implicit operator string(TSelf self);
    static abstract implicit operator TSelf(string self);
}
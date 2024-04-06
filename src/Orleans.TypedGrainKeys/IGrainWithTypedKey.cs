namespace Orleans;

public interface IGrainWithTypedKey<TGrainKey> : IGrainWithStringKey where TGrainKey : ITypedGrainKey<TGrainKey>;
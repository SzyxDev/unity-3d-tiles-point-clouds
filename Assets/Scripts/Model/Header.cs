using System;

public class Header
{
    public string Magic {get; set;}
    public UInt32 Version {get; set;}
    public UInt32 ByteLength {get; set;}
    public UInt32 FeatureTableJSONByteLength {get; set;}
    public UInt32 FeatureTableBinaryByteLength {get; set;}
    public UInt32 BatchTableJSONByteLength {get; set;}
    public UInt32 BatchTableBinaryByteLength {get; set;}
}
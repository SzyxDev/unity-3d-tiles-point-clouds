using System;
using System.IO;
using System.Text;
using UnityEngine;

public class Loader : MonoBehaviour
{
    public string tile;
    public string pntsFile;

    private const string ALLOWED_FORMAT = "pnts";
    private const int FORMAT_LENGTH = 4;

    private string _magic;
    private UInt32 _version;
    private UInt32 _byteLength;
    private UInt32 _featureTableJSONByteLength;
    private UInt32 _featureTableBinaryByteLength;
    private UInt32 _batchTableJSONByteLength;
    private UInt32 _batchTableBinaryByteLength;
    private FeatureTable _featureTable;


    private double _position;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start loading Files");
        readPntsFile();
    }

    private void readPntsFile()
    {
        if (File.Exists(pntsFile))
        {
            using (var stream = File.Open(pntsFile, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    if (!readHeader(reader))
                    {
                        Debug.LogError(_magic + " is no valid Format.");
                        return;
                    }

                    _featureTable = parseFeatureTable(reader.ReadBytes((int) _featureTableJSONByteLength));

                    Debug.Log(new String(reader.ReadChars((int) _featureTableBinaryByteLength)));

                }
            }


        }
        else
        {
            Debug.Log("File does not exists.");
        }
    }

    private bool readHeader(BinaryReader reader)
    {
        _magic = new String(reader.ReadChars(FORMAT_LENGTH));
        if (_magic != ALLOWED_FORMAT)
        {
            return false;
        }
        _version = reader.ReadUInt32();
        _byteLength = reader.ReadUInt32();
        _featureTableJSONByteLength = reader.ReadUInt32();
        _featureTableBinaryByteLength = reader.ReadUInt32();
        _batchTableJSONByteLength = reader.ReadUInt32();
        _batchTableBinaryByteLength = reader.ReadUInt32();

        logHeader();

        return true;
    }

    private void logHeader()
    {
        Debug.Log("Format: " + _magic);
        Debug.Log("Version: " + _version);
        Debug.Log("ByteLength: " + _byteLength);
        Debug.Log("FeatureTableJSONByteLength: " + _featureTableJSONByteLength);
        Debug.Log("FeatureTableBinaryByteLength: " + _featureTableBinaryByteLength);
        Debug.Log("BatchTableJSONByteLength: " + _batchTableJSONByteLength);
        Debug.Log("BatchTableBinaryByteLength: " + _batchTableBinaryByteLength);
    }

    private FeatureTable parseFeatureTable(byte[] json)
    {
        string jsonStr = Encoding.UTF8.GetString(json);

        return JsonUtility.FromJson<FeatureTable>(jsonStr);
    }

    // Update is called once per frame
    void Update()
    {

    }
}

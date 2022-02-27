using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class Loader : MonoBehaviour
{
    public string tile;
    public string pntsFile;

    private const string ALLOWED_FORMAT = "pnts";
    private const int FORMAT_LENGTH = 4;
    private const int POINT_SIZE = 3;
    private const int N_POINT_BYTES = 4;
    private const int Q_POINT_BYTES = 2;

    private string _magic;
    private UInt32 _version;
    private UInt32 _byteLength;
    private UInt32 _featureTableJSONByteLength;
    private UInt32 _featureTableBinaryByteLength;
    private UInt32 _batchTableJSONByteLength;
    private UInt32 _batchTableBinaryByteLength;
    private FeatureTable _featureTable;
    private List<IPoint<float>> _points;
    private PointCloudRenderer _pointCloudRenderer;

    private PointTypes _pointType;

    enum PointTypes
    {
        NPoint,
        QPoint
    }


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start loading Files");
        _points = new List<IPoint<float>>();
        _pointCloudRenderer = GameObject.Find("Settings").GetComponent<PointCloudRenderer>();
        readPntsFile();
    }

    /// <summary>
    /// Read the complete binary pnts file and create usable points.
    /// </summary>
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

                    if (_featureTable.POINTS_LENGTH * POINT_SIZE * N_POINT_BYTES == _featureTable.RGB.byteOffset) 
                    {
                        _pointType = PointTypes.NPoint;
                    }
                    else if (_featureTable.POINTS_LENGTH * POINT_SIZE * Q_POINT_BYTES == _featureTable.RGB.byteOffset)
                    {
                        _pointType = PointTypes.QPoint;
                    }
                    else
                    {
                        Debug.LogError("No valid point type found.");
                        return;
                    }


                    for (int i = 0; i < _featureTable.POINTS_LENGTH; i++)
                    {
                        var x = reader.ReadSingle();
                        var y = reader.ReadSingle();
                        var z = reader.ReadSingle();
                        
                        _points.Add(new NPoint(x, y, z));
                        Debug.Log(_points[i].ToString());
                        Debug.Log(i);
                    }


                    _pointCloudRenderer.RenderPoints(_points);
                }
            }


        }
        else
        {
            Debug.Log("File does not exists.");
        }
    }

    /// <summary>
    /// Read the header from the binary-file.
    /// </summary>
    /// <param name="reader">The reader containing the binary file</param>
    /// <returns>True if the header contains a valid format (magic), false if not.</returns>
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

    /// <summary>
    /// Log the header to the unity debug log.
    /// </summary>
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

    /// <summary>
    /// Parse the feature table json from given bytes.
    /// </summary>
    /// <param name="json">The json as byte array</param>
    /// <returns>Returns the feature table.</returns>
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

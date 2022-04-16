using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Loader : MonoBehaviour
{
    public string jsonTileOrFolder;

    private const string ALLOWED_FORMAT = "pnts";
    private const int FORMAT_LENGTH = 4;
    private const int POINT_SIZE = 3;
    private const int N_POINT_BYTES = 4;
    private const int Q_POINT_BYTES = 2;
    private const string TILE_SET_JSON = "tileset.json";

    private ConcurrentBag<Task> _tasks;
    private List<List<IPoint<float>>> _points;
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
        Debug.Log("Start loading files");
        _tasks = new ConcurrentBag<Task>();
        _points = new List<List<IPoint<float>>>();
        _pointCloudRenderer = GameObject.Find("Settings").GetComponent<PointCloudRenderer>();

        FileAttributes attr = File.GetAttributes(jsonTileOrFolder);
        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
        {
            foreach (string jsonTilePath in getTileJsonsFromChildFolders(jsonTileOrFolder))
            {
                _tasks.Add(Task.Run(() => readTileJson(jsonTilePath)));
            }
        }
        else
        {
            _tasks.Add(Task.Run(() => readTileJson(jsonTileOrFolder)));
        }


        while (_tasks.Any(t => !t.IsCompleted))
        {
            Task.WhenAll(_tasks.ToArray());
        }
        
        Debug.Log("Finish loading files");
        _pointCloudRenderer.RenderPoints(_points);
    }

    /// <summary>
    /// Returns a list of all the child folders of the root folder
    /// </summary>
    /// <param name="rootFolder">The root folder path</param>
    /// <returns>A list of child folder paths</returns>
    private List<string> getTileJsonsFromChildFolders(string rootFolder)
    {
        string[] folders = Directory.GetDirectories(rootFolder);
        List<string> tileJsons = new List<string>();

        foreach (string folder in folders)
        {
            tileJsons.Add(folder + "/" + TILE_SET_JSON);
        }

        return tileJsons;
    }

    /// <summary>
    /// Read a 3D-Tiles json file and create tasks to read all children 3D-Tiles files and pnts files
    /// </summary>
    /// <param name="jsonTile">The root 3D-Tiles file</param>
    private void readTileJson(string jsonTile)
    {
        if (File.Exists(jsonTile))
        {
            string json = File.ReadAllText(jsonTile, Encoding.UTF8);
            TileSet tileSet = parseTileSet(json);

            foreach (Child child in tileSet.root.children)
            {
                string childContent = Path.GetDirectoryName(jsonTile) + "/" + child.content.uri;
                if (childContent.EndsWith(ALLOWED_FORMAT))
                {
                    _tasks.Add(Task.Run(() => readPntsFile(childContent)));
                }
                else
                {
                    _tasks.Add(Task.Run(() => readTileJson(childContent)));
                }
            }
            _tasks.Add(Task.Run(() => readPntsFile(Path.GetDirectoryName(jsonTile) + "/" + tileSet.root.content.uri)));
        }
        else
        {
            Debug.Log(jsonTile + " does not exist.");
        }
    }

    /// <summary>
    /// Read the complete binary pnts file and create usable points.
    /// </summary>
    /// <param name="pntsFile">The pnts file</param>
    private void readPntsFile(string pntsFile)
    {
        if (File.Exists(pntsFile))
        {
            byte[] bytes = File.ReadAllBytes(pntsFile);
            int index = 0;
            Header header = readHeader(bytes, ref index);
            if (!ALLOWED_FORMAT.Equals(header.Magic))
            {
                Debug.LogError(header.Magic + " is no valid Format.");
                return;
            }

            FeatureTable featureTable = parseFeatureTable(readCharsAsString(bytes, ref index, (int)header.FeatureTableJSONByteLength));

            if (featureTable.POINTS_LENGTH * POINT_SIZE * N_POINT_BYTES == featureTable.RGB.byteOffset)
            {
                _pointType = PointTypes.NPoint;
            }
            else if (featureTable.POINTS_LENGTH * POINT_SIZE * Q_POINT_BYTES == featureTable.RGB.byteOffset)
            {
                _pointType = PointTypes.QPoint;
            }
            else
            {
                Debug.LogError("No valid point type found.");
                return;
            }

            List<IPoint<float>> localPoints = new List<IPoint<float>>();

            for (int i = 0; i < featureTable.POINTS_LENGTH; i++)
            {
                var x = readSingle(bytes, ref index);
                var y = readSingle(bytes, ref index);
                var z = readSingle(bytes, ref index);

                localPoints.Add(new NPoint(x, y, z));
            }

            for (int i = 0; i < featureTable.POINTS_LENGTH; i++)
            {
                var r = readByte(bytes, ref index);
                var g = readByte(bytes, ref index);
                var b = readByte(bytes, ref index);

                localPoints[i].Color = new byte[] { r, g, b };
            }

            _points.Add(localPoints);
        }
        else
        {
            Debug.Log(pntsFile + " does not exists.");
        }
    }

    /// <summary>
    /// Read the header from the binary-file.
    /// </summary>
    /// <param name="bytes">The bytes of the binary-file</param>
    /// <param name="index">The current index of the bytes</param>
    /// <returns>The header containing all the informations</returns>
    private Header readHeader(byte[] bytes, ref int index)
    {
        Header header = new Header();
        header.Magic = readCharsAsString(bytes, ref index, FORMAT_LENGTH);
        header.Version = readUInt32(bytes, ref index);
        header.ByteLength = readUInt32(bytes, ref index);
        header.FeatureTableJSONByteLength = readUInt32(bytes, ref index);
        header.FeatureTableBinaryByteLength = readUInt32(bytes, ref index);
        header.BatchTableJSONByteLength = readUInt32(bytes, ref index);
        header.BatchTableBinaryByteLength = readUInt32(bytes, ref index);

        //logHeader(header);

        return header;
    }

    /// <summary>
    /// Log the header to the unity debug log.
    /// </summary>
    /// <param name="header">The header to log</param>
    private void logHeader(Header header)
    {
        Debug.Log("Format: " + header.Magic);
        Debug.Log("Version: " + header.Version);
        Debug.Log("ByteLength: " + header.ByteLength);
        Debug.Log("FeatureTableJSONByteLength: " + header.FeatureTableJSONByteLength);
        Debug.Log("FeatureTableBinaryByteLength: " + header.FeatureTableBinaryByteLength);
        Debug.Log("BatchTableJSONByteLength: " + header.BatchTableJSONByteLength);
        Debug.Log("BatchTableBinaryByteLength: " + header.BatchTableBinaryByteLength);
    }

    /// <summary>
    /// Parse the feature table json from given json string.
    /// </summary>
    /// <param name="json">The json string/param>
    /// <returns>Returns the feature table.</returns>
    private FeatureTable parseFeatureTable(string jsonStr)
    {
        return JsonUtility.FromJson<FeatureTable>(jsonStr);
    }


    /// <summary>
    /// Parse the tile set json from given json string.
    /// </summary>
    /// <param name="json">The json string/param>
    /// <returns>Returns the tile set.</returns>
    private TileSet parseTileSet(string json)
    {
        return JsonUtility.FromJson<TileSet>(json);
    }

    /// <summary>
    /// Read the uint32 at the given bytes at the given index and increase the index accordingly
    /// </summary>
    /// <param name="bytes">The bytes of the binary-file</param>
    /// <param name="index">The current index of the bytes</param>
    /// <returns>The uint32</returns>
    private uint readUInt32(byte[] bytes, ref int index)
    {
        uint i = BitConverter.ToUInt32(bytes, index);
        index += 4;

        return i;
    }

    /// <summary>
    /// Read the float at the given bytes at the given index and increase the index accordingly
    /// </summary>
    /// <param name="bytes">The bytes of the binary-file</param>
    /// <param name="index">The current index of the bytes</param>
    /// <returns>The float</returns>
    private float readSingle(byte[] bytes, ref int index)
    {
        float f = BitConverter.ToSingle(bytes, index);
        index += 4;

        return f;
    }

    /// <summary>
    /// Return the byte at the given bytes at the given index and increase the index accordingly
    /// </summary>
    /// <param name="bytes">The bytes of the binary-file</param>
    /// <param name="index">The current index of the bytes</param>
    /// <returns>The byte</returns>
    private byte readByte(byte[] bytes, ref int index)
    {
        return bytes[index++];
    }

    /// <summary>
    /// Return the given number of bytes at the given bytes at the given index and increase the index accordingly
    /// </summary>
    /// <param name="bytes">The bytes of the binary-file</param>
    /// <param name="index">The current index of the bytes</param>
    /// <param name="numBytes">The number of bytes to read</param>
    /// <returns></returns>
    private byte[] readBytes(byte[] bytes, ref int index, int numBytes)
    {
        byte[] b = new byte[numBytes];
        Array.Copy(bytes, index, b, 0, numBytes);
        index += numBytes;

        return b;
    }

    /// <summary>
    /// Read the given number of chars at the given bytes and return them as string and increase the index accordingly
    /// </summary>
    /// <param name="bytes">The bytes of the binary-file</param>
    /// <param name="index">The current index of the bytes</param>
    /// <param name="numChars">The number of chars to read</param>
    /// <returns>The chars as a string</returns>
    private string readCharsAsString(byte[] bytes, ref int index, int numChars)
    {
        return Encoding.UTF8.GetString(readBytes(bytes, ref index, numChars));
    }

    // Update is called once per frame
    void Update()
    {

    }
}

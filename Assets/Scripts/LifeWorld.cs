using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;

public class LifeWorld : MonoBehaviour
{

    /// <summary>
    /// 周りの生きてるLifeの個数を計算するJob
    /// </summary>
    [BurstCompile]
    struct ParallelForAliveLifeCountJob : IJobParallelFor
    {
        public NativeArray<AroundStructData> aroundIndexList;
        [ReadOnly]
        public NativeArray<int> aliveBoolList;
        public NativeArray<int> envCountList;

        public void Execute(int index)
        {
            var count = 0;
            var aroundIndexs = aroundIndexList[index];
            for (int i = 0; i < 26; i++) {
                var boolListIndex = aroundIndexs.GetValue(i);
                if (boolListIndex == -1) continue;
                if (aliveBoolList[boolListIndex] != 0)
                {//生きてるLifeの数だけカウント
                    count++;
                }
            }
            envCountList[index] = count;
        }
    }
    
    /// <summary>
    /// そのフレームでLifeが生きているかどうかを判定するJob
    /// </summary>
    [BurstCompile]
    struct ParallelForLifeAliveJob : IJobParallelFor
    {
        //周りのブロックの生きてるLifeの数
        public NativeArray<int> envCountList;
        
        //Lifeが生きているかどうかの真偽値のリスト
        public NativeArray<int> lifeAliveBoolList;

        //生きてる時に、死んでしまう最小/最大の値
        public int minDeadCount;
        public int maxDeadCount;

        //死んでる時に、生まれる最小/最大の値
        public int minBirthCount;
        public int maxBirthCount;

        [ReadOnly]
        public NativeArray<float> RandomValueArray;

        public float MissRate;

        public void Execute(int index)
        {
            //値を取得
            var count = envCountList[index];
            var isAlive = lifeAliveBoolList[index];

            if (isAlive == 1)
            {//生きてる時は、死ぬかどうかの判定
                var boolAlive = (count > minDeadCount) && (count < maxDeadCount);
                isAlive = boolAlive ? 1 : 0;
                if (RandomValueArray[index] < MissRate)
                {
                    /*
                    //間違えて死んじゃう
                    if(isAlive == 0)
                    {
                        isAlive = 1;
                    }
                    /*/
                    //判定を間違えちゃう
                    isAlive = !boolAlive ? 1 : 0;
                    //*/
                }
            }
            else
            {//死んでる時は、生まれるかどうかの判定
                var boolAlive = (count >= minBirthCount) && (count <= maxBirthCount);
                isAlive = boolAlive ? 1 : 0;
            }
            
            lifeAliveBoolList[index] = isAlive;
        }
    }
    
    private static V3[] _arounds { get; set; }
    private static void DirectionsInitialize()
    {
        _arounds = new V3[26];
        var count = 0;
        for (var k = -1; k <= 1; k++)
        {
            for (var j = -1; j <= 1; j++)
            {
                for (var i = -1; i <= 1; i++)
                {
                    if (i == 0 && j == 0 && k == 0) continue;
                    _arounds[count] = new V3(i, j, k);
                    count++;
                }
            }
        }
    }
    
    public Transform _prefab;
    
    [SerializeField]
    private int _xWidth = 10;
    [SerializeField]
    private int _yWidth = 10;
    [SerializeField]
    private int _zWidth = 10;
    [SerializeField]
    private bool _xDoRound = true;
    [SerializeField]
    private bool _yDoRound = true;
    [SerializeField]
    private bool _zDoRound = true;

    private MeshRenderer[] _meshRendererArray;
    private GameObject[] _gameObjectArray;

    //生きてる時に、死んでしまう最小/最大の値
    public int MinDeadCount;
    public int MaxDeadCount;

    //死んでる時に、生まれる最小/最大の値
    public int MinBirthCount;
    public int MaxBirthCount;

    [Range(0.0f,1.0f)]
    public float birthThreshold;

    [Range(0.0f, 1.0f)]
    public float MissRate;


    private int _length;
    // Start is called before the first frame update
    void Start()
    {
        DirectionsInitialize();
        _length = _xWidth * _yWidth * _zWidth;
        _gameObjectArray = new GameObject[_length];
        _meshRendererArray = new MeshRenderer[_length];
        _allBoolData = new NativeArray<int>(_length,Allocator.Persistent);
        var _aroundLifesIndexDataArray = new AroundStructData[_length];
        _aroundIndexList = new NativeArray<AroundStructData>(_length, Allocator.Persistent);
        //i*j*kの直方体区間にLifeを配置
        for (int k = 0; k < _zWidth; k++)
        {
            for (int j = 0; j < _yWidth; j++)
            {
                for (int i = 0; i < _xWidth; i++)
                {
                    var Pos = new V3(i, j, k);
                    var thisLifesIndex = Pos.GetIndex(_xWidth,_yWidth);
                    var tr = Instantiate(_prefab, new Vector3(Pos.X, Pos.Y, Pos.Z), transform.rotation);
                    _gameObjectArray[thisLifesIndex] = tr.gameObject;
                    _meshRendererArray[thisLifesIndex] = tr.GetComponent<MeshRenderer>();
                    bool startIsAlive = Random.value > birthThreshold;

                    if (!startIsAlive)
                    {
                        if (!isTest) _gameObjectArray[thisLifesIndex].SetActive(false);
                        else _meshRendererArray[thisLifesIndex].enabled = false;
                    }
                    _allBoolData[thisLifesIndex] = startIsAlive ? 1 : 0;
                    _aroundLifesIndexDataArray[thisLifesIndex] = new AroundStructData(new int[26]);
                    _aroundIndexList[thisLifesIndex] = new AroundStructData(new int[26]);
                    for (var aroundsIndex = 0; aroundsIndex < _arounds.Length; aroundsIndex++)
                    {
                        V3 newPos = Pos + _arounds[aroundsIndex];
                        _aroundLifesIndexDataArray[thisLifesIndex].SetValue(aroundsIndex, newPos.GetRoundIndex(_xWidth, _yWidth,_zWidth,_xDoRound,_yDoRound,_zDoRound));
                        _aroundIndexList[thisLifesIndex] = _aroundLifesIndexDataArray[thisLifesIndex];
                    }
                }
            }
            /*
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            //*/
        }
    }
    private NativeArray<int> _allBoolData;
    public bool isTest;
    private System.Diagnostics.Stopwatch sw;
    private NativeArray<AroundStructData> _aroundIndexList;
    private void OnDestroy()
    {
        _aroundIndexList.Dispose();
        _allBoolData.Dispose();
    }
    void Update()
    {
        /*
        sw.Stop();
        Debug.Log("0::" + sw.ElapsedMilliseconds + "ms");
        sw.Restart();
        //*/
        var outputEnvCountListBuffer = new NativeArray<int>(_length, Allocator.TempJob);
        var job1 = new ParallelForAliveLifeCountJob()
        {
            aroundIndexList = _aroundIndexList,
            aliveBoolList = _allBoolData,
            envCountList = outputEnvCountListBuffer
        };
        var handler1 = job1.Schedule(_length, 0);
        handler1.Complete();
        
        var randomValueArrayBuffer = new NativeArray<float>(_length, Allocator.TempJob);
        for(int i = 0; i < _length; i++)
        {
            randomValueArrayBuffer[i] = Random.value;
        }
        var job2 = new ParallelForLifeAliveJob() {
            envCountList = outputEnvCountListBuffer,
            lifeAliveBoolList = _allBoolData,
            minDeadCount = MinDeadCount,
            maxDeadCount= MaxDeadCount,
            minBirthCount = MinBirthCount,
            maxBirthCount = MaxBirthCount,
            RandomValueArray = randomValueArrayBuffer,
            MissRate = MissRate
        };
        var handler2 = job2.Schedule(_length, 0);
        handler2.Complete();
        outputEnvCountListBuffer.Dispose();
        randomValueArrayBuffer.Dispose();

        if (!isTest)
        {
            for (int i = 0; i < _length; i++)
            {
                _gameObjectArray[i].SetActive(_allBoolData[i] == 1);
            }
        }
        else
        {
            for (int i = 0; i < _length; i++)
            {
                _meshRendererArray[i].enabled = (_allBoolData[i] == 1);
            }
        }
        /*
        sw.Stop();
        Debug.Log("4::" + sw.ElapsedMilliseconds + "ms");
        sw.Restart();
        //*/
    }
}
public struct V3
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public V3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    public static V3 operator +(V3 a, V3 b)
    {
        return new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }
    public int GetIndex(int xWidth, int yWidth)
    {
        return this.X + this.Y * xWidth + this.Z * xWidth * yWidth;
    }
    public int GetRoundIndex(int xWidth, int yWidth,int zWidth,bool xDoRound,bool yDoRound,bool zDoRound)
    {
        var x = GetRoundValue(this.X, xWidth, xDoRound);
        if (x == -1) return -1;
        var y = GetRoundValue(this.Y, yWidth, yDoRound);
        if (y == -1) return -1;
        var z = GetRoundValue(this.Z, zWidth, zDoRound);
        if (z == -1) return -1;
        return new V3(x,y,z).GetIndex(xWidth,yWidth);
    }
    public int GetRoundValue(int value, int roundThreshold, bool doRound)
    {
        if (value >= roundThreshold)
        {
            if (!doRound) return -1;
            return GetRoundValue(value - roundThreshold, roundThreshold,doRound);
        }
        if (value < 0)
        {
            if (!doRound) return -1;
            return GetRoundValue(value + roundThreshold, roundThreshold,doRound);
        }
        return value;
    }
}

public struct AroundStructData
{
    public int mimjmk;
    public int mjmk;
    public int imjmk;

    public int mimk;
    public int mk;
    public int imk;

    public int mijmk;
    public int jmk;
    public int ijmk;

    public int mimj;
    public int mj;
    public int imj;

    public int mi;
    public int i;

    public int mij;
    public int j;
    public int ij;

    public int mimjk;
    public int mjk;
    public int imjk;

    public int mik;
    public int k;
    public int ik;

    public int mijk;
    public int jk;
    public int ijk;

    public int Length;
    public AroundStructData(int[] arounds)
    {
        mimjmk = arounds[0];
        mjmk = arounds[1];
        imjmk = arounds[2];
        mimk = arounds[3];
        mk = arounds[4];
        imk = arounds[5];
        mijmk = arounds[6];
        jmk = arounds[7];
        ijmk = arounds[8];
        mimj = arounds[9];
        mj = arounds[10];
        imj = arounds[11];
        mi = arounds[12];
        i = arounds[13];
        mij = arounds[14];
        j = arounds[15];
        ij = arounds[16];
        mimjk = arounds[17];
        mjk = arounds[18];
        imjk = arounds[19];
        mik = arounds[20];
        k = arounds[21];
        ik = arounds[22];
        mijk = arounds[23];
        jk = arounds[24];
        ijk = arounds[25];
        Length = 26;
    }
    public int GetValue(int index)
    {
        switch (index)
        {
            case 0:
                return this.mimjmk;
            case 1:
                return this.mjmk;
            case 2:
                return this.imjmk;
            case 3:
                return this.mimk;
            case 4:
                return this.mk;
            case 5:
                return this.imk;
            case 6:
                return this.mijmk;
            case 7:
                return this.jmk;
            case 8:
                return this.ijmk;
            case 9:
                return this.mimj;
            case 10:
                return this.mj;
            case 11:
                return this.imj;
            case 12:
                return this.mi;
            case 13:
                return this.i;
            case 14:
                return this.mij;
            case 15:
                return this.j;
            case 16:
                return this.ij;
            case 17:
                return this.mimjk;
            case 18:
                return this.mjk;
            case 19:
                return this.imjk;
            case 20:
                return this.mik;
            case 21:
                return this.k;
            case 22:
                return this.ik;
            case 23:
                return this.mijk;
            case 24:
                return this.jk;
            case 25:
                return this.ijk;
            default:
                return 0;
        }
    }
    public void SetValue(int index,int value)
    {
        switch (index)
        {
            case 0:
                mimjmk = value;
                break;
            case 1:
                mjmk = value;
                break;
            case 2:
                imjmk = value;
                break;
            case 3:
                mimk = value;
                break;
            case 4:
                mk = value;
                break;
            case 5:
                imk = value;
                break;
            case 6:
                mijmk = value;
                break;
            case 7:
                jmk = value;
                break;
            case 8:
                ijmk = value;
                break;
            case 9:
                mimj = value;
                break;
            case 10:
                mj = value;
                break;
            case 11:
                imj = value;
                break;
            case 12:
                mi = value;
                break;
            case 13:
                i = value;
                break;
            case 14:
                mij = value;
                break;
            case 15:
                j = value;
                break;
            case 16:
                ij = value;
                break;
            case 17:
                mimjk = value;
                break;
            case 18:
                mjk = value;
                break;
            case 19:
                imjk = value;
                break;
            case 20:
                mik = value;
                break;
            case 21:
                k = value;
                break;
            case 22:
                ik = value;
                break;
            case 23:
                mijk = value;
                break;
            case 24:
                jk = value;
                break;
            case 25:
                ijk = value;
                break;
            default:
                return;
        }
    }
}

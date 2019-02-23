using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Burst;

public class TransformAccessTest : MonoBehaviour
{
    /// <summary>
    /// バーストコンパイルあり
    /// </summary>
    [BurstCompileAttribute]
    struct TestJobBurst : IJobParallelForTransform
    {
        public NativeArray<Vector3> positions;

        public void Execute(int index, TransformAccess transform)
        {
            var pos = positions[index];
            transform.localPosition = pos;
        }
    }

    /// <summary>
    /// バーストコンパイルなし
    /// </summary>
    struct TestJobNoBurst : IJobParallelForTransform
    {
        public NativeArray<Vector3> positions;

        public void Execute(int index, TransformAccess transform)
        {
            var pos = positions[index];
            transform.localPosition = pos;
        }
    }

    public enum ExecuteType
    {
        None,
        NoBurst,
        Burst
    }

    public ExecuteType executeType;

    public Transform _prefab;

    public float distance = 5f;

    [SerializeField]
    private int _count = 10000;

    private TransformAccessArray _transformAccessArray;

    void Start()
    {
        var transforms = new Transform[_count];
        for (int i = 0; i < _count; i++)
        {
            var tr = Instantiate(_prefab);
            transforms[i] = tr;
        }

        // TODO 第2引数の意味
        // オーバーロードでキャパシティだけ入れるパターンはその後どういう感じで要素を詰める？
        _transformAccessArray = new TransformAccessArray(transforms);
    }

    private void OnDestroy()
    {
        _transformAccessArray.Dispose();
    }

    void Update()
    {
        var inputBuffer = new NativeArray<Vector3>(_transformAccessArray.length, Allocator.Temp);
        if (executeType == ExecuteType.Burst)
        {
            for (int i = 0; i < _transformAccessArray.length; i++)
            {
                inputBuffer[i] = new Vector3(Random.Range(-distance, distance), Random.Range(-distance, distance), Random.Range(-distance, distance));
            }

            var job = new TestJobBurst()
            {
                positions = inputBuffer
            };
            var handler = job.Schedule(_transformAccessArray);
            handler.Complete();
        }
        else if (executeType == ExecuteType.NoBurst)
        {
            for (int i = 0; i < _transformAccessArray.length; i++)
            {
                inputBuffer[i] = new Vector3(Random.Range(-distance, distance), Random.Range(-distance, distance), Random.Range(-distance, distance));
            }

            var job = new TestJobNoBurst()
            {
                positions = inputBuffer
            };
            var handler = job.Schedule(_transformAccessArray);
            handler.Complete();
        }
        else
        {
            // ジョブ未使用
            for (int i = 0; i < _transformAccessArray.length; i++)
            {
                _transformAccessArray[i].localPosition =
                    new Vector3(Random.Range(-distance, distance), Random.Range(-distance, distance), Random.Range(-distance, distance));
            }
        }

        inputBuffer.Dispose();
    }
}
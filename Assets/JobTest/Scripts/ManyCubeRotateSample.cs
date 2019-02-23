using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public class ManyCubeRotateSample : MonoBehaviour
{
    /// <summary>
    /// キューブを回転させるJOB
    /// </summary>
    [BurstCompile]
    struct TestParallelForTransformTestJob : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<Vector3> datList;

        [ReadOnly]
        public float deltaTime;

        public void Execute(int index, TransformAccess transform)
        {
            var currentRotation = transform.localRotation;
            var data = datList[index];
            // 現在のQuaternionに回転値を加算
            transform.localRotation = currentRotation * Quaternion.Euler(data * deltaTime);
        }
    }

    public Transform _prefab;

    /// <summary>
    /// キューブの位置
    /// </summary>
    public float distance = 5f;

    [SerializeField]
    private int _count = 10000;

    private TransformAccessArray _transformAccessArray;

    public Vector3[] angleVelocity;

    void Start()
    {
        var transforms = new Transform[_count];
        angleVelocity = new Vector3[_count];

        var angle = 100f;
        for (int i = 0; i < _count; i++)
        {
            var tr = Instantiate(_prefab);
            transforms[i] = tr;

            angleVelocity[i] = new Vector3(Random.Range(-angle, angle), Random.Range(-angle, angle),
                Random.Range(-angle, angle));
        }

        _transformAccessArray = new TransformAccessArray(transforms);

        for (int i = 0; i < _transformAccessArray.length; i++)
        {
            _transformAccessArray[i].localPosition =
                new Vector3(Random.Range(-distance, distance), Random.Range(-distance, distance),
                    Random.Range(-distance, distance));
        }
    }

    private void OnDestroy()
    {
        _transformAccessArray.Dispose();
    }

    void Update()
    {
        var inputBuffer = new NativeArray<Vector3>(_transformAccessArray.length, Allocator.TempJob);
        for (int i = 0; i < _transformAccessArray.length; i++)
        {
            inputBuffer[i] = angleVelocity[i];
        }

        var dt = Time.deltaTime;
        var job = new TestParallelForTransformTestJob()
        {
            datList = inputBuffer,
            deltaTime = dt
        };
        var handler = job.Schedule(_transformAccessArray);
        handler.Complete();


        inputBuffer.Dispose();
    }
}
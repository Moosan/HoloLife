using Unity.Burst;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// 毎フレームpositionにvelocityを加算するだけの単純なジョブサンプル
/// IJobParallelFor使用
/// ジョブ使用 / 未使用をフラグで切り替えてます
/// </summary>
public class ApplyVelocitySampleIJobParallelFor : MonoBehaviour
{
    /// <summary>
    /// ジョブ定義
    /// 定義できる変数はBlittable型のみ
    /// </summary>
    //[ComputeJobOptimizationAttribute]
    struct VelocityJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> velocity;

        public NativeArray<Vector3> position;

        public float deltaTime;

        /// <summary>
        /// ジョブの処理内容
        /// </summary>
        public void Execute(int index)
        {
            Utility.Execute(index, position, velocity, deltaTime);
        }
    }

    /// <summary>
    /// 処理する要素数
    /// </summary>
    private int _count = 100000;

    public bool useJob = false;

    public void Update()
    {
        // バッファ生成
        var position = new NativeArray<Vector3>(_count, Allocator.Persistent);
        var velocity = new NativeArray<Vector3>(_count, Allocator.Persistent);
        for (var i = 0; i < velocity.Length; i++)
        {
            // 入力バッファの中身を詰める
            velocity[i] = new Vector3(0, 10, 0);
        }

        if (useJob)
        {
            // ジョブ生成して、必要情報を渡す
            var job = new VelocityJob()
            {
                deltaTime = Time.deltaTime,
                position = position,
                velocity = velocity
            };

            // ジョブを実行
            JobHandle jobHandle = job.Schedule(_count, 0);

            // ジョブ完了の待機
            jobHandle.Complete();
        }
        else
        {
            for (int i = 0; i < _count; i++)
            {
                Utility.Execute(i, position, velocity, Time.deltaTime);
            }
        }

        for (int i = 0; i < _count; i++)
        {
            // 更新後のデータを取得してほげほげする
            var pos = position[i];
            // Do something...            
        }


        // バッファの破棄
        position.Dispose();
        velocity.Dispose();
    }
}

public static class Utility
{
    public static void Execute(int index, NativeArray<Vector3> position, NativeArray<Vector3> velocity, float deltaTime)
    {
        position[index] = position[index] + velocity[index] * deltaTime;
    }
}
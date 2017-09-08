using UnityEngine;

public class GerstnerWave : MonoBehaviour
{
    //numWaves = 4
    public float steepness = 1;
    public Vector4 amplitude = Vector4.one;
    public Vector4 waveLength = Vector4.one;
    public Vector4 speed = Vector4.one;
    public Vector2 dir1 = new Vector2(0.615f, 1);
    public Vector2 dir2 = new Vector2(0.788f, 0.988f);
    public Vector2 dir3 = new Vector2(0.478f, 0.937f);
    public Vector2 dir4 = new Vector2(0.154f, 0.71f);

    public Renderer[] m_renderers;
    // Use this for initialization
    void Start()
    {
        m_renderers = GetComponentsInChildren<Renderer>();
        SetParams(m_renderers);
    }

    void SetParams(Renderer[] renderers)
    {
        if (null == renderers)
            return;
        Vector4 Q = Vector4.zero;
        dir1.Normalize();
        dir2.Normalize();
        dir3.Normalize();
        dir4.Normalize();

        Vector4 omega = new Vector4(2 * Mathf.PI / waveLength.x, 2 * Mathf.PI / waveLength.y, 2 * Mathf.PI / waveLength.z, 2 * Mathf.PI / waveLength.w);
        Vector4 dx = new Vector4(dir1.x, dir2.x, dir3.x, dir4.x);
        Vector4 dz = new Vector4(dir1.y, dir2.y, dir3.y, dir4.y);

        Q = new Vector4(steepness / (omega.x * amplitude.x * 4), steepness / (omega.y * amplitude.y * 4), steepness / (omega.z * amplitude.z * 4), steepness / (omega.w * amplitude.w * 4));

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial.SetVector("_QA", new Vector4(Q.x * amplitude.x, Q.y * amplitude.y, Q.z * amplitude.z, Q.w * amplitude.w));
            renderers[i].sharedMaterial.SetVector("_A", amplitude);
            renderers[i].sharedMaterial.SetVector("_Dx", dx);
            renderers[i].sharedMaterial.SetVector("_Dz", dz);
            renderers[i].sharedMaterial.SetVector("_S", speed);
            renderers[i].sharedMaterial.SetVector("_W", omega);
            Shader.SetGlobalFloat("_WaterAltitude", transform.position.y);
        }
    }

    // Update is called once per frame
    void Update()
    {   
        SetParams(m_renderers);
    }
}


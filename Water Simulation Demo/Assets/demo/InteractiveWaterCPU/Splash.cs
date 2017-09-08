using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class Splash : MonoBehaviour
{

    //public SurfaceCreator surface;

    private ParticleSystem system;
    private ParticleSystem.EmissionModule emittor;
    private ParticleSystem.Particle[] particles;
    List<Vector3> splashRegion;

    void Start()
    {
        splashRegion = InteractiveWaterCPU.splashRegion;
    }
    
    private void Update()
    {
        if (system == null)
        {
            system = GetComponent<ParticleSystem>();
        }
        if (particles == null || particles.Length < system.main.maxParticles)
        {
            particles = new ParticleSystem.Particle[system.main.maxParticles];
        }
        emittor = system.emission;
        emittor.enabled = false;

        if (splashRegion.Count != 0)
        {
            emittor.enabled = true;
            int particleCount = system.GetParticles(particles);
            PositionParticles();
            system.SetParticles(particles, particleCount);
        } else
        {
            emittor.enabled = false;
            system.Clear();
        }
    }

    private void PositionParticles()
    {
        for (int i=0, j=0; i< particles.Length; i++,j++)
        {
            if (j == splashRegion.Count) j = 0;

            Vector3 position = particles[i].position;
            position.x = splashRegion[j].x + Random.Range(-0.2f, 0.2f);
            position.y = splashRegion[j].y;
            position.z = splashRegion[j].z + Random.Range(-0.2f, 0.2f);
            particles[i].position = position;
        }
    }

}

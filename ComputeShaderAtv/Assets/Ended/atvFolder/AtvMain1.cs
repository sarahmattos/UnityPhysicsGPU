using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtvMain1 : MonoBehaviour
{ 
   
    public float dynamicSphereRadiusMin;
    public float dynamicSphereRadiusMax; 
    public Vector3 minSpace;
    public Vector3 maxSpace;
    public int interactions = 100;
    float totalTime = 0;
    int currentInteraction = 0;

    public float minSpeed = 10;
    public float maxSpeed = 20;
    public float minMass = 10;
    public float maxMass = 20;
    public int numParticles = 10;
    public bool useGPU = true;

    public ComputeShader computeShader;
    ComputeBuffer cbDynamicSphere;

    // Struct to keep particles info
    struct Sphere
    {
        public float radius;
        public Vector3 position;
        public float V;
        public float mass;
        public Vector3 direction;
        public Color color;
        public int aux;
    }

    Sphere[] bufferDynamicSpheres;
    Transform[] dynamicSpheres;


    // Start is called before the first frame update
    void Start()
    {
        
        bufferDynamicSpheres = new Sphere[numParticles];
        dynamicSpheres = new Transform[numParticles];

        ParticleSpawn();

        // Add the dynamic particles to a buffer in order to process with the physics
        for (int i = 0; i < numParticles; i++)
        {
            Color _colorInic = Random.ColorHSV();
            float radius = Random.Range(dynamicSphereRadiusMin, dynamicSphereRadiusMax);
            bufferDynamicSpheres[i] = new Sphere();
            bufferDynamicSpheres[i].radius = radius;
            bufferDynamicSpheres[i].V = Random.Range(minSpeed, maxSpeed);
            bufferDynamicSpheres[i].mass = Random.Range(minMass, maxMass);
            bufferDynamicSpheres[i].position = dynamicSpheres[i].position;
            bufferDynamicSpheres[i].color = _colorInic;
            bufferDynamicSpheres[i].aux = 1;
            bufferDynamicSpheres[i].direction = dynamicSpheres[i].rotation * Vector3.forward;
            dynamicSpheres[i].localScale = new Vector3(radius, radius, radius);
        }

       

        if (useGPU)
        {
            int totalsize = sizeof(float) * 3 + sizeof(float) * 6 + sizeof(float) * 4 + sizeof(int);

            cbDynamicSphere = new ComputeBuffer(bufferDynamicSpheres.Length, totalsize);
            cbDynamicSphere.SetData(bufferDynamicSpheres);
            computeShader.SetBuffer(0, "dynamicSphere", cbDynamicSphere);
            computeShader.SetVector("minWorld", new Vector4(minSpace.x, minSpace.y, minSpace.z, 0));
            computeShader.SetVector("maxWorld", new Vector4(maxSpace.x, maxSpace.y, maxSpace.z, 0));
            computeShader.SetInt("dElements", bufferDynamicSpheres.Length);

        }

    }

    // Update is called once per frame
    void Update()
    {
        float begin = Time.realtimeSinceStartup;

        if (useGPU)
        {
            GPUSim();
        }
        else
        {
            // Perform the simulation in CPU
            CPUSim();
        }

        totalTime += Time.realtimeSinceStartup - begin;
        currentInteraction++;

        if (currentInteraction == interactions)
        {
            float _mean = totalTime / interactions;
            Debug.Log("Mean (ms): " + _mean);
        }

    }


    private void CPUSim()
    {
        for (int i = 0; i < dynamicSpheres.Length; i++)
        {
            Transform _ds = dynamicSpheres[i];

            float aceleration = 9.8f;
            float F = bufferDynamicSpheres[i].mass * aceleration;

            if (_ds.position.y - bufferDynamicSpheres[i].radius > minSpace.y)
            {
                _ds.Translate(new Vector3(0, 0, (bufferDynamicSpheres[i].V + (F / bufferDynamicSpheres[i].mass) * Time.deltaTime) * Time.deltaTime));
            }
            //Debug.Log(bufferDynamicSpheres[i].aux);
            if (_ds.position.y - bufferDynamicSpheres[i].radius < minSpace.y && bufferDynamicSpheres[i].aux < 2)
            {
                bufferDynamicSpheres[i].color = Random.ColorHSV();
                dynamicSpheres[i].GetComponent<Renderer>().material.SetColor("_Color", bufferDynamicSpheres[i].color);
                bufferDynamicSpheres[i].aux++;


            }
        }

    }



    private void GPUSim()
    {
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetInt("interactions", interactions);
        computeShader.Dispatch(0, Mathf.CeilToInt(numParticles / 10.0f), 1, 1);
        cbDynamicSphere.GetData(bufferDynamicSpheres);

        for (int i = 0; i < bufferDynamicSpheres.Length; i++)
        {
            dynamicSpheres[i].position = bufferDynamicSpheres[i].position;
            if (bufferDynamicSpheres[i].aux==2)
            {
                dynamicSpheres[i].GetComponent<Renderer>().material.SetColor("_Color", bufferDynamicSpheres[i].color);
            }
           
        }

    }

    private void OnDestroy()
    {
        if (useGPU)
        {
            cbDynamicSphere.Dispose();
        }
    }

    // Particle spawner
    private void ParticleSpawn()
    {        
        int spanwedParticles = 0;
        float currentZ = minSpace.z + dynamicSphereRadiusMax * 4;
        float currentX = minSpace.x + dynamicSphereRadiusMax * 4;
        float currentY = maxSpace.y + dynamicSphereRadiusMax * 4;

        while (spanwedParticles < numParticles) 
        {
            Color _colorInic = Random.ColorHSV();
            GameObject _sp = GameObject.CreatePrimitive(PrimitiveType.Sphere);  
            _sp.transform.Rotate(90f, 0f, 0f);
            _sp.GetComponent<Renderer>().material.color = Random.ColorHSV();
            _sp.transform.position = new Vector3(currentX, currentY, currentZ);
            dynamicSpheres[spanwedParticles] = _sp.transform;            

            currentX += dynamicSphereRadiusMax * 4;

            if (currentX > maxSpace.x - dynamicSphereRadiusMax * 4)
            {
                currentX = minSpace.x + dynamicSphereRadiusMax * 4;
                currentZ += dynamicSphereRadiusMax * 4;
            }
            
            spanwedParticles++;
        }
    }
}

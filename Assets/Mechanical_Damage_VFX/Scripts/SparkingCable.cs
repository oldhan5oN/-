using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MechDamage
{

    public class SparkingCable : MonoBehaviour
    {

        public float minInterval = 1f;
        public float maxInterval = 5f;
        public float minForce = 200f;
        public float maxForce = 2000f;

        public GameObject sparksVFX;

        public ParticleSystem sparkSmoke;
        public ParticleSystem strikeSpark;
        public ParticleSystem fallSparks;
        public ParticleSystem streakSparks;
        public ParticleSystem electricity;

        public GameObject cable;

        public GameObject constructedCable;

        public GameObject cableSingleSection;

        public AudioSource Electriciy_Spark_Audio_A;
        public AudioSource Electriciy_Spark_Audio_B;
        public AudioSource Electriciy_Spark_Audio_C;
        public AudioSource Electriciy_Spark_Audio_D;

        private GameObject objectToFind;
        private Rigidbody rb;
        private string cableName;
        private bool begin = false;
        private GameObject lastChildObject;

        void Start()
        {

            cableSingleSection.SetActive(true);

        }

        void Update()
        {

            if (begin == false)
            {
                StartCoroutine("AttachVFXToCable");
            }

        }



        IEnumerator RandomCoroutine()
        {

            // Start spark VFX
            sparkSmoke.Play();
            strikeSpark.Play();
            fallSparks.Play();
            streakSparks.Play();
            electricity.Play();


            // Select random sound effect

            int sparkAudioRnd = (Random.Range(1, 5));

            print(sparkAudioRnd);

            if (sparkAudioRnd == 1)
            {
                Electriciy_Spark_Audio_A.Play();
                sparkAudioRnd = 0;
            }

            if (sparkAudioRnd == 2)
            {
                Electriciy_Spark_Audio_B.Play();
                sparkAudioRnd = 0;
            }

            if (sparkAudioRnd == 3)
            {
                Electriciy_Spark_Audio_C.Play();
                sparkAudioRnd = 0;
            }

            if (sparkAudioRnd == 4)
            {
                Electriciy_Spark_Audio_D.Play();
                sparkAudioRnd = 0;
            }


            // Add random force to cable end
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, Random.Range(0, 360), transform.eulerAngles.z);
            float speed = Random.Range(minForce, maxForce);
            rb.isKinematic = false;
            Vector3 force = transform.forward;
            force = new Vector3(force.x, 1, force.z);
            rb.AddForce(force * speed);

            yield return new WaitForSeconds(0.1f);

        }

        IEnumerator AttachVFXToCable()
        {


            begin = true;

            yield return new WaitForSeconds(1);

            // Attach sparking VFX to end of the cable
            cableName = cable.name;
            string nameOfCable = cableName + "/" + "Cable_Joined" + "/" + "1";
            objectToFind = GameObject.Find(nameOfCable);
            sparksVFX.transform.parent = objectToFind.transform;
            sparksVFX.transform.localPosition = Vector3.zero;

            // Hide first cable segment (this segment is often glitchy and problematic)
            int lastChildIndex = constructedCable.transform.childCount - 1;
            lastChildObject = constructedCable.transform.GetChild(lastChildIndex).gameObject;
            lastChildObject.GetComponent<MeshRenderer>().enabled = false;

            rb = objectToFind.GetComponent<Rigidbody>();

            cableSingleSection.SetActive(false);

            while (true)
            {

                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                StartCoroutine("RandomCoroutine");

            }

        }


    }


}


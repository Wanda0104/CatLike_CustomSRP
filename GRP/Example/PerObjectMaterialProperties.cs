using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;
namespace GRP.Example
{
    public class PerObjectMaterialProperties : MonoBehaviour
    {
        private static int baseColorId = Shader.PropertyToID("_BaseColor"),
            cutoffId = Shader.PropertyToID("_Cutoff"),
            metallicId = Shader.PropertyToID("_Metallic"),
            smoothnessId = Shader.PropertyToID("_Smoothness"),
            emissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Color baseColor = Color.white;
        [SerializeField, Range(0f, 1f)] private float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;

        [SerializeField, ColorUsage(false, true)]
        private Color emissionColor = Color.black;

        static MaterialPropertyBlock _materialPropertyBlock;

        // Start is called before the first frame update
        void Start()
        {

        }

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            _materialPropertyBlock ??= new MaterialPropertyBlock();

            _materialPropertyBlock.SetColor(baseColorId, baseColor);
            _materialPropertyBlock.SetFloat(cutoffId, cutoff);
            _materialPropertyBlock.SetFloat(metallicId, metallic);
            _materialPropertyBlock.SetFloat(smoothnessId, smoothness);
            _materialPropertyBlock.SetColor(emissionColorId, emissionColor);
            gameObject.GetComponent<Renderer>().SetPropertyBlock(_materialPropertyBlock);
        }

        [Button("RandomProperty")]
        public void RandomProperty()
        {
            baseColor = Random.ColorHSV();
            baseColor.a = Random.value;
            cutoff = Random.Range(0f, 1f);
            emissionColor = Random.ColorHSV();
            OnValidate();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
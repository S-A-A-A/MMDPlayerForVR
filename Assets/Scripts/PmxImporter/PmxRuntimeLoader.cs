using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using MMDPlayerForVR.PmxImporter.Parsers;
using MMDPlayerForVR.PmxImporter.Builders;

namespace MMDPlayerForVR.PmxImporter
{
    /// <summary>
    /// Runtime Entry Point for importing PMX models directly in Unity.
    /// Can be attached to a GameObject for easy testing in the Inspector.
    /// </summary>
    public class PmxRuntimeLoader : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Absolute path to the .pmx file to load (e.g. C:/Models/Miku/miku.pmx)")]
        public string pmxFilePath = "";
        
        [Tooltip("If true, automatically loads the model when the scene starts")]
        public bool loadOnStart = false;

        private PmxImporterPipeline _pipeline;

        private void Awake()
        {
            // Set up the DI container manually for this entry point
            var parser = new PmxParser();
            var meshBuilder = new PmxMeshBuilder();
            var boneBuilder = new PmxBoneBuilder();
            var materialBuilder = new PmxMaterialBuilder();
            var physicsBuilder = new PmxPhysicsBuilder();

            _pipeline = new PmxImporterPipeline(
                parser, 
                meshBuilder, 
                boneBuilder, 
                materialBuilder, 
                physicsBuilder
            );
        }

        private async void Start()
        {
            if (loadOnStart && !string.IsNullOrEmpty(pmxFilePath))
            {
                await LoadModelAsync(pmxFilePath);
            }
        }

        /// <summary>
        /// Call this method from UI or other scripts to trigger the load at runtime.
        /// </summary>
        public async Task<GameObject> LoadModelAsync(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[PmxRuntimeLoader] PMX file not found at path: {path}");
                return null;
            }

            Debug.Log($"[PmxRuntimeLoader] Starting import of {path} ...");
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();

            GameObject model = await _pipeline.ImportAsync(path);

            stopWatch.Stop();
            if (model != null)
            {
                Debug.Log($"[PmxRuntimeLoader] Import completed successfully in {stopWatch.ElapsedMilliseconds} ms.");
                // Attach the loaded model to this GameObject to keep the scene clean
                model.transform.SetParent(this.transform, false);
            }
            else
            {
                Debug.LogError("[PmxRuntimeLoader] Import failed. Please check the console logs for details.");
            }

            return model;
        }
    }
}

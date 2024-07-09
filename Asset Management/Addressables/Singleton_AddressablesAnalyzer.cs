using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace QuizCanners.SavageTurret {
    public class Singleton_AddressablesAnalyzer : Singleton.BehaniourBase, IPEGI
    {
        private async Task ChildTask()
        {
            await Task.CompletedTask;

            await Task.Yield();

            try
              {
                UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
                await handle.Task;

                if (handle.IsValid() && handle.IsDone)
                {
                    Debug.Log(handle.Result.ToString() + " Loaded");
                }
                else
                {
                    Debug.Log("Failed to load");
                }

            } catch (Exception ex) 
            {
                 Debug.LogException(ex);
             
            }

        }


        private async Task FitstLaunch() 
        {
            Debug.Log("First Launch");

            await SecondLaunch();

            await Task.CompletedTask;
        }

        private async Task SecondLaunch()
        {
            Debug.Log("Second Launch");
            await Task.CompletedTask;
        }

        private async Task TestTask()
        {

            var tsks = new List<Task>();

            tsks.Add(FitstLaunch());

            tsks.Add(SecondLaunch());

            for (int i = 0; i < 100; i++)
            {
                try
                {
                    await ChildTask();
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                }
                Debug.Log("Frame Index: " + Time.frameCount);
            }

            await Task.WhenAll(tsks);
        }

        private Task _testTask;
        private string address;

        public override void Inspect()
        {
            pegi.Nl();

            "Test".PegiLabel(50).Edit(ref address).Nl();

            if (_testTask == null)
                "Run Task".PegiLabel().Click().OnChanged(()=> _testTask = TestTask());
            else
            {
                _testTask.Status.ToString().PegiLabel().Nl();
                "Clear Task".PegiLabel().Click().OnChanged(()=> _testTask = null);
                
            }
        }
    }

    [PEGI_Inspector_Override(typeof(Singleton_AddressablesAnalyzer))] internal class AddressablesAnalyzerServiceDrawer : PEGI_Inspector_Override { }
}

using UnityEngine;

public class SetTargetFramerate : MonoBehaviour
{
   private void Awake()
   {
      QualitySettings.vSyncCount = 0;
      Application.targetFrameRate = 120;
   }
}
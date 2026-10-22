using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class StartButton : MonoBehaviour
{
    [System.Serializable]
    public class ThisIsButton
    {
        public Button Button;
        public string SceneName;
    }
    [SerializeField] public List<ThisIsButton> ThisIsButtonList;

    private void Start()
    {


        foreach (var buttonData in ThisIsButtonList)
        {
            string sceneName = buttonData.SceneName;

            buttonData.Button.onClick.RemoveAllListeners();
            buttonData.Button.onClick.AddListener(
                () => CallLoadScene(sceneName)
            );
        }
    }
    public void CallLoadScene(string sceneName)
    {
        
       SceneManager.LoadScene(sceneName);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject   ButtonHP,
                        ButtonWCh,
                        ButtonKostka,
                        ButtonSzczerbcowa,
                        ButtonBackFromChoice,
                        OpenEntrancesButton,
                        ChooseBuildingMenu,
                        ChooseOptionMenu,
                        ChooseSettingsMenu,
                        OpenSettingsButton,
                        ShowHiddenPoints,
                        ShowCurrentScenePoints,
                        B1S, B0, B1, B2, B3, B4, B5;
    // Nazwa sceny
    [SerializeField] private string HenrykaPoboznego1S = "HP1";
    [SerializeField] private string HenrykaPoboznego0 = "HPP0";
    [SerializeField] private string HenrykaPoboznego1 = "HPP1";
    [SerializeField] private string HenrykaPoboznego2 = "HPP2";
    [SerializeField] private string HenrykaPoboznego3 = "HPP3";
    [SerializeField] private string HenrykaPoboznego4 = "HPP4";
    [SerializeField] private string HenrykaPoboznego5 = "HPP5";
    [SerializeField] private string WalyChrobrego1S = "WCh1S";
    [SerializeField] private string WalyChrobrego0 = "WCh0";
    [SerializeField] private string WalyChrobrego1 = "WChP1";
    [SerializeField] private string WalyChrobrego2 = "WChP2";
    [SerializeField] private string WalyChrobrego3 = "WChP3";
    [SerializeField] private string WalyChrobrego4 = "WChP4";
    [SerializeField] private string Kostka1S = "Kostka1S";
    [SerializeField] private string Kostka0 = "Kostka0";
    [SerializeField] private string Kostka1 = "Kostka1";
    [SerializeField] private string Szczerbcowa0 = "SzczerbcowaParter";
    [SerializeField] private string Szczerbcowa1 = "SzczerbcowaPietro";

    public int buildingType = 0;

    public void EnterHP()
    {
        buildingType = 0;
        TurnItAllOff();
        ChooseBuildingMenu.SetActive(true);
        ButtonBackFromChoice.SetActive(true);
        B1S.SetActive(true);
        B0.SetActive(true);
        B1.SetActive(true);
        B2.SetActive(true);
        B3.SetActive(true);
        B4.SetActive(true);
        B5.SetActive(true);
    }
    public void EnterWCh()
    {
        buildingType = 1;
        TurnItAllOff();
        ChooseBuildingMenu.SetActive(true);
        ButtonBackFromChoice.SetActive(true);
        B1S.SetActive(true);
        B0.SetActive(true);
        B1.SetActive(true);
        B2.SetActive(true);
        B3.SetActive(true);
        B4.SetActive(true);
    }
    public void EnterKostka()
    {
        buildingType = 2;
        TurnItAllOff();
        ChooseBuildingMenu.SetActive(true);
        ButtonBackFromChoice.SetActive(true);
        B1S.SetActive(true);
        B0.SetActive(true);
        B1.SetActive(true);
    }
    public void EnterSzczerbcowa()
    {
        buildingType = 3;
        TurnItAllOff();
        ChooseBuildingMenu.SetActive(true);
        ButtonBackFromChoice.SetActive(true);
        B0.SetActive(true);
        B1.SetActive(true);
    }
    public void EnterFloor1S()
    {
        LoadScene(buildingType+90);
    }
    public void EnterFloor0()
    {
        LoadScene(buildingType);
    }
    public void EnterFloor1()
    {
        LoadScene(buildingType+10);
    }
    public void EnterFloor2()
    {
        LoadScene(buildingType+20);
    }
    public void EnterFloor3()
    {
        LoadScene(buildingType+30);
    }
    public void EnterFloor4()
    {
        LoadScene(buildingType+40);
    }
    public void EnterFloor5()
    {
        LoadScene(buildingType+50);
    }
    public void LoadScene(int BI)
    {
        switch (BI)
        {
            case 90:
                SceneManager.LoadScene(HenrykaPoboznego1S);
                break;
            case 0:
                SceneManager.LoadScene(HenrykaPoboznego0);
                break;
            case 10:
                SceneManager.LoadScene(HenrykaPoboznego1);
                break;
            case 20:
                SceneManager.LoadScene(HenrykaPoboznego2);
                break;
            case 30:
                SceneManager.LoadScene(HenrykaPoboznego3);
                break;
            case 40:
                SceneManager.LoadScene(HenrykaPoboznego4);
                break;
            case 50:
                SceneManager.LoadScene(HenrykaPoboznego5);
                break;

            case 91:
                SceneManager.LoadScene(WalyChrobrego1S);
                break;
            case 1:
                SceneManager.LoadScene(WalyChrobrego0);
                break;
            case 11:
                SceneManager.LoadScene(WalyChrobrego1);
                break;
            case 21:
                SceneManager.LoadScene(WalyChrobrego2);
                break;
            case 31:
                SceneManager.LoadScene(WalyChrobrego3);
                break;
            case 41:
                SceneManager.LoadScene(WalyChrobrego4);
                break;

            case 92:
                SceneManager.LoadScene(Kostka1S);
                break;
            case 2:
                SceneManager.LoadScene(Kostka0);
                break;
            case 12:
                SceneManager.LoadScene(Kostka1);
                break;

            case 3:
                SceneManager.LoadScene(Szczerbcowa0);
                break;
            case 13:
                SceneManager.LoadScene(Szczerbcowa1);
                break;
        }
    }
    public void OpenEntrances()
    {
        TurnItAllOff();
        ButtonHP.SetActive(true);
        ButtonWCh.SetActive(true);
        ButtonKostka.SetActive(true);
        ButtonSzczerbcowa.SetActive(true);
        ButtonBackFromChoice.SetActive(true);
        ChooseBuildingMenu.SetActive(true);
    }
    public void OpenSettings()
    {
        TurnItAllOff();
        ButtonBackFromChoice.SetActive(true);
        ChooseSettingsMenu.SetActive(true);
        ShowHiddenPoints.SetActive(true);
        ShowCurrentScenePoints.SetActive(true);
    }
    public void ReturnToMenu()
    {
        TurnItAllOff();
        OpenEntrancesButton.SetActive(true);
        OpenSettingsButton.SetActive(true);
        ChooseOptionMenu.SetActive(true);
    }
    void TurnItAllOff()
    {
        ButtonHP.SetActive(false);
        ButtonWCh.SetActive(false);
        ButtonKostka.SetActive(false);
        ButtonSzczerbcowa.SetActive(false);
        ButtonBackFromChoice.SetActive(false);
        ChooseBuildingMenu.SetActive(false);
        OpenEntrancesButton.SetActive(false);
        OpenSettingsButton.SetActive(false);
        ChooseOptionMenu.SetActive(false);
        ChooseSettingsMenu.SetActive(false);
        ShowHiddenPoints.SetActive(false);
        ShowCurrentScenePoints.SetActive(false);
        B1S.SetActive(false);
        B0.SetActive(false);
        B1.SetActive(false);
        B2.SetActive(false);
        B3.SetActive(false);
        B4.SetActive(false);
        B5.SetActive(false);
    }
    public void ToggleShowHidden(bool value1)
    {
        PlayerPrefs.SetInt("ShowHiddenPoints", value1 ? 1 : 0);
        PlayerPrefs.Save();
    }
    public void OnCurrentFloorOnlyToggle(bool value2)
    {
        PlayerPrefs.SetInt("StartOnlyCurrentScene", value2 ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Start()
    {
        LoadToggleStates();
    }

    void LoadToggleStates()
    {
        ShowHiddenPoints.GetComponent<Toggle>().isOn =
            PlayerPrefs.GetInt("ShowHiddenPoints", 0) == 1;

        ShowCurrentScenePoints.GetComponent<Toggle>().isOn =
            PlayerPrefs.GetInt("StartOnlyCurrentScene", 0) == 1;
    }
}
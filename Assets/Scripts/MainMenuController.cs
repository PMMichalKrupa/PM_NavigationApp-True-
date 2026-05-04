using UnityEngine;
using UnityEngine.SceneManagement;

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
                        OpenSettingsButton;
    // Nazwa sceny
    [SerializeField] private string HenrykaPoboznego = "HPP0";
    [SerializeField] private string WalyChrobrego = "WCh0";
    [SerializeField] private string Kostka = "Kostka1S";
    [SerializeField] private string Szczerbcowa = "SzczerbcowaParter";

    public void EnterHP()
    {
        SceneManager.LoadScene(HenrykaPoboznego);
    }
    public void EnterWCh()
    {
        SceneManager.LoadScene(WalyChrobrego);
    }
    public void EnterKostka()
    {
        SceneManager.LoadScene(Kostka);
    }
    public void EnterSzczerbcowa()
    {
        SceneManager.LoadScene(Szczerbcowa);
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
    }
}
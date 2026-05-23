using UnityEngine;
using UnityEngine.UI;

public class SwitchIcon : MonoBehaviour
{
    public GameObject firstImage;
    public GameObject secondImage;

    private bool isFirst = true;
    public void SwitchImage()
    {
        if (firstImage)
        {
            firstImage.SetActive(true);
            secondImage.SetActive(false);
        }
        else
        {
            secondImage.SetActive(true);
            firstImage.SetActive(false);
        }
        isFirst = !isFirst;
    }
}

using UnityEngine;

public class LineBetweenObjects : MonoBehaviour
{
    private RectTransform object1;
    private RectTransform object2;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetObjects(GameObject one, GameObject two)
    {
        object1 = one.GetComponent<RectTransform>();
        object2 = two.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (!object1 || !object2) return;
        if (!object1.gameObject.activeSelf || !object2.gameObject.activeSelf) return;

        Vector3 dif = object2.localPosition - object1.localPosition;
        if (dif.sqrMagnitude < 0.01f) return;

        rectTransform.localPosition = (object1.localPosition + object2.localPosition) / 2f;
        rectTransform.sizeDelta = new Vector2(dif.magnitude, 5f);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dif.y, dif.x) * Mathf.Rad2Deg);
    }
}

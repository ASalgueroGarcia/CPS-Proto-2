/*
 * Original code is not mine. Here's where I got it:
 * https://discussions.unity.com/t/any-good-way-to-draw-lines-between-ui-elements/575979/3
 */

using UnityEngine;
using UnityEngine.UI;

public class LineBetweenObjects : MonoBehaviour
{
    private RectTransform _object1;
    private RectTransform _object2;
    private Image _image;
    private RectTransform _rectTransform;

    private void Start()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
    }

    public void SetObjects(GameObject one, GameObject two)
    {
        _object1 = one.GetComponent<RectTransform>();
        _object2 = two.GetComponent<RectTransform>();

        if (!(_object1.localPosition.x > _object2.localPosition.x)) return;
        (_object1, _object2) = (_object2, _object1);
    }

    private void Update()
    {
        if (!_object1 || !_object2) return;
        if (!_object1.gameObject.activeSelf || !_object2.gameObject.activeSelf) return;
        _rectTransform.localPosition = (_object1.localPosition + _object2.localPosition) / 2;
        var dif = _object2.localPosition - _object1.localPosition;
        _rectTransform.sizeDelta = new Vector3(dif.magnitude, 5);
        _rectTransform.rotation = Quaternion.Euler(new Vector3(0, 0, 180 * Mathf.Atan(dif.y / dif.x) / Mathf.PI));
    }
}
using UnityEngine;
public enum Speaker
{
    Merchant = 0,
    Sarah = 1,
    Narrator = 2
}

public struct Dialogue
{
    public uint id;
    public Speaker speaker;
    public string text;
    public bool viewed;
}

public class DialogueTest : MonoBehaviour
{
    private ShuffleBag<Dialogue> dialogues = new ShuffleBag<Dialogue>();
    
    void Start()
    {
        dialogues.Add(new Dialogue { id = 1, speaker = Speaker.Merchant, text = "Hello!", viewed = false });
        dialogues.Add(new Dialogue { id = 2, speaker = Speaker.Merchant, text = "Hi there!", viewed = false });
        dialogues.Add(new Dialogue { id = 3, speaker = Speaker.Merchant, text = "Good morning!", viewed = false });

        for (int i = 0; i < 5; i++)
        {
            Dialogue nextDialogue = dialogues.GetNext();
            Debug.Log($"{nextDialogue.speaker}: '{nextDialogue.text}'");
        }
    }

    void OnDestroy()
    {
        dialogues.Clear();
    }
}

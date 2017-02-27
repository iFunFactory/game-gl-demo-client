using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Field2Portal : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.gameObject.name.Equals("unitychan"))
            return;
        // 캐릭터 동작 멈춤
        UnityChanControlScriptWithRgidBody.wait = true;

        // Scene 로드
        SceneManager.LoadScene("Scenes/Field2");
        GameLogic.Instance.ClearOtherPlayersDictionary();
        GameLogic.Instance.SetInstanceState(GameLogic.INSTANCESTATE.WAIT);

        Dictionary<string, object> message = new Dictionary<string, object>();
        message["field_index"] = 2;
        NetworkManager.Instance.Send("portal", message);
    }
}

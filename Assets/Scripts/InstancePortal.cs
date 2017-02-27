using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstancePortal : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.gameObject.name.Equals("unitychan"))
            return;
        // 캐릭터 동작 멈춤
        UnityChanControlScriptWithRgidBody.wait = true;
        // 캐릭터 이동
        UnityChanControlScriptWithRgidBody.myCharacter.transform.localPosition = new Vector3(UnityChanControlScriptWithRgidBody.myCharacter.transform.localPosition.x, UnityChanControlScriptWithRgidBody.myCharacter.transform.localPosition.y, UnityChanControlScriptWithRgidBody.myCharacter.transform.localPosition.z - 6);
        // 방향 전환
        UnityChanControlScriptWithRgidBody.myCharacter.transform.localEulerAngles = new Vector3(UnityChanControlScriptWithRgidBody.myCharacter.transform.localEulerAngles.x, 180, UnityChanControlScriptWithRgidBody.myCharacter.transform.localEulerAngles.z);
        // 캐릭터 위치 보고
        Dictionary<string, object> message = new Dictionary<string, object>();
        UnityChanControlScriptWithRgidBody.myCharacter.AddPositionInfoToDictionary(message);
        message["Name"] = NetworkManager.Instance.myId;
        NetworkManager.Instance.Send("relay", message);

        // 인스턴스 Scene 로드
        SceneManager.LoadScene("Scenes/Instance");
        GameLogic.Instance.ClearOtherPlayersDictionary();
        GameLogic.Instance.SetInstanceState(GameLogic.INSTANCESTATE.WAIT);

        // 인스턴스 이동
        message.Clear();
        NetworkManager.Instance.Send("dungeon", message);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GolemAnimationController : MonoBehaviour
{
    public static GolemAnimationController Instance;

    Animation anim;
    string playAnimationName;
    bool isClearSent = false;

    void Start()
    {
        anim = GetComponent<Animation>();
        anim.Stop();
        playAnimationName = "rage";
        anim.Play(playAnimationName);

        Instance = this;
    }

    public void PlayAnimation(string animationName)
    {
        playAnimationName = animationName;
        anim.Play(playAnimationName);
    }

    void Update()
    {
        switch(playAnimationName)
        {
            case "idle":
                if (!anim.isPlaying)
                {
                    if (Random.Range(0, 100) < 20)
                        playAnimationName = "idle_action";
                    anim.Play(playAnimationName);
                }
                break;
            case "idle_action":
                if (!anim.isPlaying)
                {
                    playAnimationName = "idle";
                    anim.Play(playAnimationName);
                }
                break;
            case "rage":
                if (!anim.isPlaying)
                {
                    playAnimationName = "idle";
                    anim.Play(playAnimationName);
                }
                break;
            case "die":
                if (!anim.isPlaying && !isClearSent)
                {
                    isClearSent = true;
                    SceneManager.LoadScene("Scenes/Field");
                    NetworkManager.Instance.Send("clear");
                }
                break;
        }
    }
}

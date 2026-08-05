using UnityEngine;

public class ShieldBlock : MonoBehaviour
{
    public static bool IsBlocking = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemyWeapon"))
        {
            IsBlocking = true;

            Debug.Log("BLOCKED");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("EnemyWeapon"))
        {
            IsBlocking = false;
        }
    }
}
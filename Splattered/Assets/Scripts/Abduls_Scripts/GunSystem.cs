/*
------------------------------------------
By: Abdul Ahad Naveed
Created: 5/5/2023
Updated: 5/20/2023 @ 4:46 pm

Used to be a gun system
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enumerations
public enum FireMode {
    FullAuto,
    SemiAuto,
    ShotGun,
};

public class GunSystem : Tool {
    // ---------------------- PUBLIC VARIABLES ----------------------
    // General Gun Settings
    [Header("General Settings")]
    public FireMode fireMode = FireMode.SemiAuto; // FullAuto, Semi, etc.
    public float fireRate = .3f; // Fire rate every second
    public int maxCapacity = 30; // Max capacity of the gun (i.e. mag capacity, clip capacity, etc.)
    public float reloadTime = 2f; // How long it takes to reload
    public bool chamberBehavior = false; // To enable chambering behavior (if using animations, make sure u make them!)
    public bool autoReload = true; // If the gun is empty (0) it will automatically reload when Mouse0 is pressed
    public bool unequipReload = true; // If true, the gun will automatically reload if it was unequipped for at least the lenght of the reload time
    public float chamberTime = 1f; // How long it takes to chamber a round


    // Accuracy Settings
    [Header("Accuracy Settings")]
    public float inaccuracyBase = 1f; // Used in conjuction with 'inaccuractDistance': base / distance inaccuracy
    public float inaccuracyDistance = 5f; // Used in conjuction with 'inaccuracyBase': base / distance inaccuracy
    public float bloom = .1f; // Every time the gun fires, increase inaccuracyBase by this value. Can be 0.
    public float bloomReliefWait = 2f;
    public float boolRelief = .3f; // Every bloomReliefWait * Firerate while the gun is not firing, decrease bloom by this much.
    public float maxBloom = 1f; // Max bloom


    // Recoil Settings
    [Header("Recoil Settings")]
    public Vector2 recoilGripPosOffset; // How much positional offset applied each shot
    public Vector3 recoilGripRotOffset; // How much rotational offset applied each shot
    public float recoilTime = .05f; // The total time the recoil will last for until it starts relieving (make it small)
    [Range(0, 2)]
    public float recoilRelief = .1f; // The greater, the faster the offsets will go away
    [Range(0, 2)]
    public float recoilStrength= .1f; // The greater, the faster the offsets will be affected


    // Damage Settings
    [Header("Damage Settings")]
    public float baseDamage = 30f; // Base damage the gun will do
    public float headshotMultiplier = 2f; // Multiplier of the base damage for headshots


    // Bullet Settings
    [Header("Bullet Settings")]
    public float bulletVelocity = 5f; // Speed of the bullet
    public float bulletKnockback = 1f; // Knockback of the bullet
    public GameObject bulletPrefab; // To the bullet the gun is going to shoot


    // Important Objects
    [Header("Important Objects")]
    public GameObject fireObject; // The object where bullets, effects, etc. will come from


    // Sounds
    [Header("Audio")]
    public AudioClip gunShotAudio;
    [Range(0, 1)]
    public float gunShotVolume = .5f;


    // Optional Settings
    [Header("Optional Settings")]
    public Transform bolt; // Bolt that goes back when shooting
    public float boltBackValue; // Value the bolt will go back when shooting
    public float realReloadTime; // If you want the gun to have ammo at a specified time instead of at the end of the full reload
    public float realChamberTime; // If you want the gun to be chambered at a specified time instead of at the end of the full chamber
    public GunAnimations gunAnims; // Set this if you have a GunAnimation script setup for this gun
    public GameObject spentShell; // Shell that will come out every time you shoot
    public GameObject fakeBullet; // A fake bullet that will dissapear when the currentCapacity becomes 0; assumes it becomes active with an animation
    public ParticleSystem shotParticle; // Particles for the gun shot
    public GameObject genericHitParticle; // Particle for when a bullet hits the ground or the wall
    public GameObject enemyHitParticle; // Particle for when a bullet hits an enemy

    // ---------------------- PRIVATE VARIABLES ----------------------
    // Gun Status
    [HideInInspector] public int currentCapacity;
    //private int currentCapacity;
    private float lastShotTime;
    private float currentBloom;
    private float lastEquipped;
    private bool ready;
    private bool reloading;
    private bool chambering;
    private bool mouse0Down;
    private bool recoilEnabled;
    private bool needsChambering = true;
    private bool started = false;

    // Other
    private AudioSource soundEmitter; // Place to have audio
    private GameObject player; // Player Game Object
    private HandManager handManager; // Hand Manger of the Player

    // Offsets for player
    private Vector2 mainGripPosOffset = new Vector2(0, 0);
    private Vector3 mainGripRotOffset = new Vector3(0, 0, 0);

    // Start is called before the first frame update
    void Start() {
        // Setting values
        started = true;
        player = GameObject.FindGameObjectWithTag("Player");
        handManager = player.GetComponent<HandManager>();
        soundEmitter = player.GetComponent<AudioSource>();
        currentCapacity = maxCapacity;
        lastShotTime = fireRate;
        ready = true;
        needsChambering = false;
        currentBloom = 0;
        lastEquipped = 0;
        handManager.currToolLength = toolLength;
    }

    // Update is called once per frame
    void Update() {
        if (!equipped) {
            return; 
        } else {
            lastEquipped = Time.time;
        }

        // Mouse Down
        if (Input.GetKey(KeyCode.Mouse0)) {
            mouse0Down = true;

            // If using a shotgun and has ammo, it will cancel the reload if there is one
            if (reloading && currentCapacity > 0 && fireMode == FireMode.ShotGun) {
                CancelInvoke();
                if (gunAnims) {
                    gunAnims.cancel();
                }
                reloading = false;
                ready = true;
            }
        } else {
            mouse0Down = false;
        }

        // Firing
        lastShotTime += Time.deltaTime;
        if (lastShotTime >= fireRate) {
            if (ready) {
                if ((fireMode == FireMode.SemiAuto) && Input.GetKeyDown(KeyCode.Mouse0)) {
                    shoot(); // Semi-Auto
                } else if ((fireMode == FireMode.FullAuto) && mouse0Down) {
                    shoot(); // Full-Auto
                } else if ((fireMode == FireMode.ShotGun) && Input.GetKeyDown(KeyCode.Mouse0)) {
                    shoot(); // Shotgun
                }
            }
        }

        // Chacking for bloom relief
        if ((lastShotTime >= (fireRate * bloomReliefWait)) && (currentBloom > 0)) {
            lastShotTime = fireRate;
            currentBloom -= boolRelief;
            if (currentBloom < 0) {
                currentBloom = 0;
            }
        }

        // Reloading
        if (ready) {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Mouse1)) {
                if (currentCapacity < maxCapacity) {
                    reload();
                }
            }

            // Auto Reload
            if (mouse0Down && autoReload && !reloading) {
                if (currentCapacity == 0) {
                    reload();
                }
            }
        }

        // Setting Values in Hand Manager
        if (recoilEnabled) {
            mainGripPosOffset = Vector2.Lerp(mainGripPosOffset, recoilGripPosOffset, recoilStrength / 2);
            mainGripRotOffset = Vector3.Lerp(mainGripRotOffset, recoilGripRotOffset, recoilStrength / 2);
        } else {
            mainGripPosOffset = Vector2.Lerp(mainGripPosOffset, new Vector2(0, 0), recoilRelief / 2);
            mainGripRotOffset = Vector3.Lerp(mainGripRotOffset, new Vector3(0, 0, 0), recoilRelief / 2);
            if (bolt && !reloading && !chambering) {
                bolt.localPosition = Vector3.Lerp(bolt.localPosition, new Vector3(0, 0, 0), .15f);
            }
            if (ready && !reloading && !chambering && currentCapacity > 0 && needsChambering && fireMode == FireMode.ShotGun) {
                chamber();
            }
        }
        handManager.rightGripOffset = mainGripPosOffset;
        handManager.rightRotationOffset = mainGripRotOffset;
    }

    // Used to reset some stats (going to be used later when unequipping)
    void reset() {
        ready = false;
        reloading = false;
        chambering = false;
        recoilEnabled = false;
        mainGripPosOffset = new Vector2(0, 0);
        mainGripRotOffset = new Vector3(0, 0, 0);
        handManager.resetRightOffsets();
        lastShotTime = fireRate;
        CancelInvoke();
        if (bolt) {
            bolt.localPosition = new Vector3(0, 0, 0);
        }
        if (gunAnims) {
            gunAnims.cancel();
        }
        if (fakeBullet && currentCapacity == 0) {
            fakeBullet.SetActive(false);
        }
    }

    // Unequip
    public override void unequip() {
        equipped = false;
        reset();
        handManager.leftHandGrip = new Vector2(0, 0);
        handManager.leftHandGripRotOffset = new Vector3(0, 0, 0);
        handManager.yAdjust = 0;
        handManager.currToolLength = 0;
        gameObject.SetActive(false);
    }

    // Equip
    public override void equip() {
        // Just in case it didnt get a chance to start yet
        if (!started) {
            Start();
        }

        // Auto-Reload
        if (((Time.time - lastEquipped) >= reloadTime) && unequipReload) {
            if (fakeBullet) {
                fakeBullet.SetActive(true);
            }
            if (fireMode == FireMode.ShotGun) {
                if (currentCapacity < maxCapacity) {
                    currentCapacity++;
                }
            } else {
                currentCapacity = maxCapacity;
            }
        }

        // Other Equip Stuff
        gameObject.SetActive(true);
        equipped = true;
        reset();
        ready = true;
        handManager.leftHandGrip = leftHandGrip;
        handManager.leftHandGripRotOffset = leftHandGripRotOffset;
        handManager.resetLeftOffsets();
        handManager.yAdjust = yAdjust;
        handManager.currToolLength = toolLength;
        if (needsChambering && chamberBehavior) {
            if (ready && !chambering && (currentCapacity > 0)) {
                chamber();
                return;
            }
        }
    }
    
    // Chambers a round
    private void chamber() {
        chambering = true;
        ready = false;
        if (bolt) {
            bolt.localPosition = new Vector3(0, 0, 0);
        }
        if (gunAnims) {
            gunAnims.chamber(chamberTime);
        }
        float finishTime = chamberTime;
        if (realChamberTime > 0.01) {
            finishTime = realChamberTime;
        }
        Invoke("finishChamber", finishTime);
    }

    // Shoots one bullet
    private void shoot() {
        // Checking Bullets and Chambering
        if (currentCapacity == 0) { return; }
        if (needsChambering && chamberBehavior) {
            if (ready && !chambering) {
                chamber();
                return;
            }
        }
        currentCapacity--;
        if (currentCapacity == 0 || fireMode == FireMode.ShotGun) { 
            needsChambering = true;
            if (fakeBullet) {
                fakeBullet.SetActive(false);
            }
        }
        if (shotParticle) {
            shotParticle.Play();
        }

        // Setting Values
        lastShotTime = 0;

        // Effects 
        soundEmitter.pitch = Random.Range(.9f, 1.1f);
        soundEmitter.PlayOneShot(gunShotAudio, gunShotVolume);
        if (bolt) {
            bolt.localPosition = new Vector3(boltBackValue, 0, 0);
        }
        if (spentShell && (fireMode != FireMode.ShotGun)) {
            GameObject newShell = GameObject.Instantiate(spentShell, transform.position, transform.rotation);
            Destroy(newShell, 5);
        }

        // Fixing offsets
        Vector3 rightRot = handManager.rightHand.transform.localEulerAngles;
        rightRot.z -= mainGripRotOffset.z;
        handManager.rightHand.transform.localEulerAngles = rightRot;
        mainGripPosOffset = new Vector2(0, 0);
        mainGripRotOffset = new Vector3(0, 0, 0);
        handManager.resetRightOffsets();
        
        // Shooting Bullet
        int repeat = 0;
        if (fireMode == FireMode.ShotGun) {
            repeat = 4;
        }
        for (int i = -1; i < repeat; i++) {
            Vector3 position = fireObject.transform.position;
            Vector3 endPosition = fireObject.transform.position + fireObject.transform.right * inaccuracyDistance;
            float totalInaccuracy = currentBloom + inaccuracyBase;
            endPosition += transform.up * Random.Range(totalInaccuracy * -1, totalInaccuracy);
            GameObject newBullet = Instantiate(bulletPrefab, position, transform.rotation);
            newBullet.transform.right = endPosition - newBullet.transform.position;

            // Setting Bullet Values
            Bullet bulletInfo = newBullet.GetComponent<Bullet>();
            bulletInfo.baseDamage = baseDamage;
            bulletInfo.bulletVelocity = bulletVelocity;
            bulletInfo.headshotMultiplier = headshotMultiplier;
            bulletInfo.bulletKnockback = bulletKnockback;
            bulletInfo.genericHitParticle = genericHitParticle;
            bulletInfo.enemyHitParticle = enemyHitParticle;
        }
        
        // Recoil
        recoilEnabled = true;
        Invoke("finishRecoil", recoilTime);

        // Changing Bloom
        if (currentBloom < maxBloom) {
            currentBloom += bloom;
            if (currentBloom > maxBloom) {
                currentBloom = maxBloom;
            }
        }
    }

    // To Reload
    private void reload() {
        if (reloading) { return; }
        reloading = true;
        ready = false;
        if (bolt) {
            bolt.localPosition = new Vector3(0, 0, 0);
        }
        if (gunAnims) {
            gunAnims.reload(reloadTime, (maxCapacity - currentCapacity));
        }
        float finishTime = reloadTime;
        if (realReloadTime > 0.01) {
            finishTime = realReloadTime;
        }
        Invoke("finishReload", finishTime);
    }

    // Used when reloading is done
    private void finishReload() {
        reloading = false;
        if (fireMode == FireMode.ShotGun) {
            currentCapacity++;
            if (currentCapacity < maxCapacity) {
                reload();
            } else {
                ready = true;
            }
        } else {
            currentCapacity = maxCapacity;
            if (needsChambering && chamberBehavior) {
                chamber();
            } else {
                ready = true;
            }
        }
    }

    // Used for finishing recoil
    private void finishRecoil() {
        recoilEnabled = false;
    }

    // Used for finishing chambering
    private void finishChamber() {
        if (spentShell && (fireMode == FireMode.ShotGun)) {
            GameObject newShell = GameObject.Instantiate(spentShell, transform.position, transform.rotation);
            Destroy(newShell, 5);
        }
        needsChambering = false;
        chambering = false;
        ready = true;
    }
}

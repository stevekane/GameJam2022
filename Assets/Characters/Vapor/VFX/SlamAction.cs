using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlamAction : MonoBehaviour {
  public GameObject Piece;
  public float PieceLength = 1;
  public Timeval PieceActivateDelay;
  public Action<Transform, Defender> OnHit;
  float NextPieceZ = 0;
  List<GameObject> Pieces;
  bool DoneRaising;

  void Awake() {
    Pieces = new();
    Pieces.Add(Piece);
  }

  public void AddPiece() {
    NextPieceZ += PieceLength;
    var nextPiece = Instantiate(Piece, transform, false);
    nextPiece.transform.localPosition += new Vector3(0, 0, NextPieceZ);
    Pieces.Add(nextPiece);
  }

  public void Activate() {
    StartCoroutine(RaiseAll());
  }

  IEnumerator RaiseAll() {
    DoneRaising = false;
    transform.SetParent(null, true);
    for (int i = 0; i < Pieces.Count; i++) {
      StartCoroutine(RaisePiece(i));
      yield return new WaitForSeconds(PieceActivateDelay.Seconds);
    }
    yield return new WaitUntil(() => DoneRaising);
    yield return new WaitForSeconds(PieceActivateDelay.Seconds);
    Destroy(gameObject);
  }

  IEnumerator RaisePiece(int i) {
    var raiseFrames = Mathf.Max(1, PieceActivateDelay.Ticks/3);
    var yScale = 5f;
    var raiseDelta = new Vector3(0, yScale / raiseFrames, 0);

    var hitbox = Pieces[i].GetComponent<AttackHitbox>();
    hitbox.TriggerEnter = OnContact;
    hitbox.Collider.enabled = true;
    hitbox.Collider.gameObject.layer = gameObject.layer;
    for (int j = 0; j < raiseFrames; j++) {
      Pieces[i].transform.localScale += raiseDelta;
      yield return new WaitForFixedUpdate();
    }
    yield return new WaitForSeconds(PieceActivateDelay.Seconds * 4);
    for (int j = 0; j < raiseFrames; j++) {
      Pieces[i].transform.localScale -= raiseDelta;
      yield return new WaitForFixedUpdate();
    }
    if (i == Pieces.Count-1)
      DoneRaising = true;
  }

  void OnContact(Transform other) {
    if (other.TryGetComponent(out Defender defender))
      OnHit?.Invoke(transform, defender);
  }
}

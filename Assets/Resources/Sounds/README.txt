Drop your own sound clips here to override the procedural ones in SoundManager.cs.

Name each file after its cue key (extension can be .wav, .mp3, or .ogg):

  click.wav       - lobby/UI click
  join.wav        - a player joins the room
  roundstart.wav  - host hits Start Game
  tick.wav        - a new question/round/turn appears
  countdown.wav   - last 3 seconds of a timer, once per second
  reveal.wav      - answer/round reveal
  victory.wav     - final scoreboard fanfare
  go.wav          - Chosen One's pre-round verb flash (DODGE!/FIRE!/...)
  success.wav     - Chosen One win stamp
  fail.wav        - Chosen One fail stamp

SoundManager looks here first (Resources.Load<AudioClip>("Sounds/<key>")) and
only falls back to synthesizing a tone if nothing's found, so you can replace
these one at a time - anything you haven't added yet keeps working as-is.

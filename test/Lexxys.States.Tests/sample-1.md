## Sample-1 statechart diagram


``` mermaid.
stateDiagram-v2
    [*] --> Ready
    Ready --> Aborted: / Abort
    Ready --> Uploading: / Begin
    Uploading --> On_Hold: / Hold
    Uploading --> Uploading: / Reupload
    On_Hold --> Uploading: / Resume
    On_Hold --> Ready: / Restart
    Uploading --> Completed
    Completed --> [*]
    Aborted --> [*]

    state Uploading {

        [*] --> Waiting.1
        Waiting.1 --> Uploaded.1: / Upload
        Uploaded.1 --> Verified.1: / Verify
        Verified.1 --> [*]

        --
        [*] --> Waiting.2
        Waiting.2 --> Uploaded.2: / Upload
        Uploaded.2 --> Verified.2: / Verify
        Verified.2 --> [*]
    }
```
// https://github.com/vrm-c/UniVRM/blob/ad8a938e504fa5490ef04629d091ae2ea6f3f969/Assets/VRM/Samples~/SimpleViewer/Plugins/OpenFile.jslib

mergeInto(LibraryManager.library, {
    WebBrowserVrm0Open: function () {
        // TODO: more elegant code
        if (!window.vrmDowngraderObjectUrls) {
            window.vrmDowngraderObjectUrls = [];
        }
        for (const objectUrl of window.vrmDowngraderObjectUrls) {
            URL.revokeObjectURL(objectUrl);
        }
        window.vrmDowngraderObjectUrls = [];

        const fileInputId = "vrm1-file-input";
        var fileInput = document.getElementById(fileInputId);
        if (!fileInput) {
            fileInput = document.createElement("input");
            document.body.appendChild(fileInput);
        }
        fileInput.setAttribute("type", "file");
        fileInput.setAttribute("id", fileInputId);
        fileInput.setAttribute("accept", ".vrm")
        fileInput.style.visibility = "hidden";
        fileInput.onclick = function (event) {
            event.target.value = null;
        };
        fileInput.onchange = function (event) {
            var objectUrl = URL.createObjectURL(event.target.files[0]);
            window.vrmDowngraderObjectUrls.push(objectUrl);
            SendMessage("VrmDowngrader", "WebBrowserVrm0Opened", objectUrl);
        }
        fileInput.click();
    },

    WebBrowserVrm1Save: function (unityBytes, unityBytesLength) {
        // TODO: more elegant code
        if (!window.vrmDowngraderObjectUrls) {
            window.vrmDowngraderObjectUrls = [];
        }
        for (const objectUrl of window.vrmDowngraderObjectUrls) {
            URL.revokeObjectURL(objectUrl);
        }
        window.vrmDowngraderObjectUrls = [];

        var bytes = HEAPU8.subarray(unityBytes, unityBytes + unityBytesLength);
        var blob = new Blob([bytes], { type: "application/octet-stream" });

        const downloadAnchorId = "vrm0-download-anchor";
        var downloadAnchor = document.getElementById(downloadAnchorId);
        if (!downloadAnchor) {
            downloadAnchor = document.createElement("a");
            document.body.appendChild(downloadAnchor);
        }
        downloadAnchor.download = "VRM0.vrm";
        downloadAnchor.rel = "noopener";
        var objectUrl = URL.createObjectURL(blob);
        window.vrmDowngraderObjectUrls.push(objectUrl);
        downloadAnchor.href = objectUrl;
        downloadAnchor.click();
    },
});

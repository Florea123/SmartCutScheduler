/**
 * Camera helpers for the SmartCut HaircutAI page.
 * Provides camera stream access, photo capture, and cleanup utilities.
 */

window.SmartCutCamera = {
    _stream: null,

    /**
     * Start the camera stream and attach it to a <video> element by its ID.
     * Returns true on success, false if the browser denied permission.
     */
    startCamera: async function (videoElementId) {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: "user", width: { ideal: 640 }, height: { ideal: 480 } }
            });
            SmartCutCamera._stream = stream;
            const video = document.getElementById(videoElementId);
            if (video) {
                video.srcObject = stream;
                await video.play();
            }
            return true;
        } catch (e) {
            console.error("Camera access denied:", e);
            return false;
        }
    },

    /**
     * Stop the active camera stream.
     */
    stopCamera: function () {
        if (SmartCutCamera._stream) {
            SmartCutCamera._stream.getTracks().forEach(t => t.stop());
            SmartCutCamera._stream = null;
        }
    },

    /**
     * Capture a frame from a <video> element and return it as a base64 JPEG data URL.
     */
    capturePhoto: function (videoElementId) {
        const video = document.getElementById(videoElementId);
        if (!video) return null;
        const canvas = document.createElement("canvas");
        canvas.width = video.videoWidth || 640;
        canvas.height = video.videoHeight || 480;
        const ctx = canvas.getContext("2d");
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        return canvas.toDataURL("image/jpeg", 0.9);
    },

    /**
     * Check if the browser supports camera access.
     */
    isCameraSupported: function () {
        try {
            return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
        } catch (e) {
            return false;
        }
    }
};

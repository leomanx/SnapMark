chrome.runtime.onMessage.addListener((message, sender, reply) => {
  if (message?.type !== 'capture-page' || sender.id !== chrome.runtime.id) return;
  capture().then(reply).catch(error => reply({ok:false,message:error.message}));
  return true;
});
async function capture() {
  let target;
  try {
    const [tab] = await chrome.tabs.query({active:true,currentWindow:true});
    if (!tab?.id || !/^https?:/.test(tab.url ?? '')) throw new Error('Open a regular HTTP/HTTPS webpage first.');
    target = {tabId:tab.id};
    await chrome.debugger.attach(target,'1.3');
    const metrics = await chrome.debugger.sendCommand(target,'Page.getLayoutMetrics');
    const density = await chrome.debugger.sendCommand(target,'Runtime.evaluate',{expression:'window.devicePixelRatio',returnByValue:true});
    const dpr = Number(density.result?.value) || 1;
    const rect = metrics.cssContentSize ?? metrics.contentSize;
    const width = Math.ceil(rect.width), height = Math.ceil(rect.height);
    if (width < 1 || height < 1 || width*dpr > 16000 || height*dpr > 16000 || width*height*dpr*dpr > 64000000) throw new Error('Page exceeds 64 MP / 16000 px limits. Capture a smaller part instead.');
    const shot = await chrome.debugger.sendCommand(target,'Page.captureScreenshot',{format:'png',captureBeyondViewport:true,fromSurface:true,clip:{x:0,y:0,width,height,scale:1}});
    const header = Uint8Array.from(atob(shot.data.slice(0,48)), c=>c.charCodeAt(0));
    const view = new DataView(header.buffer); const pxWidth = view.getUint32(16), pxHeight = view.getUint32(20);
    if (pxWidth > 16000 || pxHeight > 16000 || pxWidth*pxHeight > 64000000) throw new Error('Rendered page exceeds image pixel limit.');
    await chrome.debugger.detach(target); target = null;
    await chrome.downloads.download({url:`data:image/png;base64,${shot.data}`,filename:`SnapMark-page-${Date.now()}.png`,saveAs:true});
    return {ok:true,message:`Saved ${pxWidth} × ${pxHeight} px locally. Open the PNG in SnapMark.`};
  } finally { if (target) { try { await chrome.debugger.detach(target); } catch {} } }
}

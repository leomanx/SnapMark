const button = document.querySelector('#capture');
const status = document.querySelector('#status');
button.addEventListener('click', async () => {
  button.disabled = true;
  status.textContent = 'Capturing full webpage…';
  try { const result = await chrome.runtime.sendMessage({type:'capture-page'}); status.textContent = result.message; }
  catch (error) { status.textContent = error.message; }
  finally { button.disabled = false; }
});
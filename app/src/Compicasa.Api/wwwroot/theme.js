/* Runs in <head> before the page paints (Session 21): applies the saved colour scheme and language,
   so a light or dark choice doesn't flash the other one first. app.js keeps them in step after that. */
(function(){
  var d=document.documentElement,th='auto',lg=null;
  try{th=localStorage.getItem('compicasa-theme')||'auto';lg=localStorage.getItem('compicasa-lang');}catch(e){}
  if(th==='light'||th==='dark')d.setAttribute('data-theme',th);
  var m=document.querySelector('meta[name="color-scheme"]');
  if(m)m.setAttribute('content',th==='light'||th==='dark'?th:'light dark');
  if(lg==='en')d.setAttribute('lang','en');
})();

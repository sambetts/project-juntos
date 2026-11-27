/* Runs in <head> before the page paints (Session 21): applies the saved colour scheme and language,
   so a light or dark choice doesn't flash the other one first. app.js keeps them in step after that. */
(function(){
  var d=document.documentElement,th='auto',lg=null;
  try{th=localStorage.getItem('compicasa-theme')||'auto';lg=localStorage.getItem('compicasa-lang');}catch(e){}
  if(th==='light'||th==='dark')d.setAttribute('data-theme',th);
  var m=document.querySelector('meta[name="color-scheme"]');
  if(m)m.setAttribute('content',th==='light'||th==='dark'?th:'light dark');
  /* No saved choice: the browser's languages, in order of preference; Spanish (or Catalan, Galician, Basque) gives
     Spanish, anything else English (Session 37). app.js uses the same rule. */
  if(lg!=='en'&&lg!=='es'){lg='en';var ls=(navigator.languages&&navigator.languages.length?navigator.languages:[navigator.language||'']);
    for(var i=0;i<ls.length;i++){var c=String(ls[i]||'').toLowerCase().split('-')[0];if(c==='en'){break;}if(c==='es'||c==='ca'||c==='gl'||c==='eu'){lg='es';break;}}}
  d.setAttribute('lang',lg);
})();

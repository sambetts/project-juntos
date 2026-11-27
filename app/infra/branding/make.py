# Session 37: the sign-in pages' images for Entra External ID company branding, drawn from wwwroot/icons/icon.svg.
# Limits (Microsoft Learn, "Add your company branding"): banner logo 245x36 PNG <=10 KB, square logo 240x240 <=50 KB,
# favicon PNG/ICO <=5 KB, background 1920x1080 <=300 KB. Run: python make.py (needs Pillow and Segoe UI).
from PIL import Image, ImageDraw, ImageFont
BRAND=(179,55,14,255); WHITE=(255,255,255,255)
def icon(size):
    s=size/512; im=Image.new('RGBA',(size,size),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([0,0,size-1,size-1],radius=112*s,fill=BRAND)
    pts=[(256,104),(92,240),(136,240),(136,408),(232,408),(232,312),(280,312),(280,408),(376,408),(376,240),(420,240)]
    d.polygon([(x*s,y*s) for x,y in pts],fill=WHITE)
    for cx in (214,298): d.ellipse([(cx-26)*s,(236-26)*s,(cx+26)*s,(236+26)*s],fill=BRAND)
    return im
def big(size,factor=4):  # draw large, scale down for smooth edges
    return icon(size*factor).resize((size,size),Image.LANCZOS)
big(240).save('square-logo.png',optimize=True)
big(32).save('favicon.png',optimize=True)
W,H=245,36; im=Image.new('RGBA',(W*4,H*4),(0,0,0,0))
im.paste(icon(H*4),(0,0))
f=ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf',int(26*4))
d=ImageDraw.Draw(im); d.text((H*4+10*4,H*4//2),'Compicasa',font=f,fill=(27,34,48,255),anchor='lm')
im.resize((W,H),Image.LANCZOS).save('banner-logo.png',optimize=True)
import os; [print(n,os.path.getsize(n)) for n in ('square-logo.png','favicon.png','banner-logo.png')]

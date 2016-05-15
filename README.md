# etf-grid (RA seminarski)

Tim:
  - Ahmed Popović
  - Benjamin Ramić
  - Muhamed Parić
  - Sanil Musić

Za praktični dio je razvijen jedan primitivni grid sistem pod nazivom „etf-grid“ koji može poslužiti za demonstraciju osnovnih principa i prednosti grid computinga. Ovaj proof-of-concept softver je razvijen da rješava jedan *specifičan* zadatak, dakle ova grid računarska mreža se može koristiti samo u tu jednu, ranije definiranu svrhu.  

Konkretno, postavljen je sljedeći zadatak/problem kojeg je potrebno riješiti:

> Na satelitskoj mapi jednog dijela grada Sarajeva (od Socijalnog do Vječne vatre) razmjere 1:25000, uslikanoj jednog dana u maju 2012. godine, potrebno je naći auto čiji je krov obojen pink bojom (RGB 0xfd0af7). Mapa je data kao ogromni .png fajl dimenzija 12288x4096.

Krećući se kroz sliku piksel po piksel, po redovima, lijevo prema desno, jednom računaru srednje jačine treba oko 50 sekundi da nađe pink auto. Inače, ono se nalazi parkirano ispred Skenderije u donjem desnom dijelu mape. Očito bi bilo korisno kada bi se više računara moglo uposliti da traže to auto, a svaki računar da pretražuje samo određeni dio mape. Time bi ubrzali proces traženja pink auta.

Shodno tome, odlučeno je da će se mapa podijeliti na 12 dijelova veličine 2048x2048 piksela, te će se razviti jednostavan grid sistem sa client-server modelom, gdje će server razdjeljivati poslove konektovanim klijentima. Komunikacija između klijenta i servera će se odvijati preko porta 5643 sa jednostavnim „custom“ protokolom, dijelom prikazanim na Ilustraciji 8.  

Koje su osobine *etf-grid* sistema?

**Server**:
-	TCP protokol, socket komunikacija
-	Mogućnost komunikacije sa više klijenata istovremeno
-	Multithreaded (thread po klijentu)
-	Nema lokalnu kopiju mape, u biti je logika dijeljenja poslova klijentima hard-kodirana u sistem (odnosno, server zna da postoji 12 dijelova nečega i da je rezultat oblika uređene trojke (x koordinata, y koordinata, broj dijela)
-	Ping-pong vid održavanja konekcije i „razaznavanja“ da je klijent još tu
-	Jednostavan prikaz trenutno konektovanih klijenata i rezultata njihovog rada

**Klijent**:
-	TCP protokol, socket komunikacija 
-	Posjeduje lokalnu kopiju mape (odnosno 12 dijelova mape)
-	Informacija o trenutnoj zauzetosti CPU-a računara

Server razdjeljuje poslove slobodnim klijentima podjednako, s tim što zadnji konektovani klijent nekada može dobiti najviše posla. To se dešava jer je implementacija razdjeljivanja poslova dosta jednostavna *(linije 187-218 u Form1.cs)*. Ova situacija se, recimo, dešava kada je konektovano 5 klijenata. Prva četiri klijenta će dobiti po 2 dijela, dok će zadnji klijenat dobiti 4 dijela posla. 

Vrijeme nalaženja pink auta se - korištenjem ovog grid sistema - očekivano smanjuje. Sa dva klijenta je vrijeme nalaženja skoro upolovljeno. Sa tri, kraće je skoro tri puta.  

Postoji određeni dodatni „overhead“: bespotrebna ping-pong komunikacija i određeno kašnjenje pri slanju poruka serveru i primanju poruka od servera, kojeg očito ne bi bilo da klijent sam obavlja posao. No, ipak, demonstrirani pristup rješavanju ovog problema se isplati.

U /bin folderu se nalazi kompajlirana verzija koja radi samo na localhostu, ali se kod može lahko izmijeniti da radi i remote. Kako probati demonstraciju na svom računaru, bez drugih računara ?

1.	Prvo se uvjeriti da je u folderu u kojem je client prisutan i folder sarajevo sa 12 .png  fajlova, a i crosshair.png koji će označiti pronađeno auto (ako bude pronađeno) klijentu
2.	Pokrenuti server
3.	Pokrenuti dva clienta i na oba kliknuti Connect dugme. Ukoliko se ne pojavi upravo konektovani klijent u prozoru servera u listi konektovanih klijenata, potrebno je ponovo pritisnuti Connect dugme
4.	U serveru pritisnuti Work dugme
5.	Sačekati rezultat


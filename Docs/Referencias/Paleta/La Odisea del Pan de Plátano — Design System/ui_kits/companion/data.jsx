window.ODISEA = {
  actos: [
    { k: 'Prólogo', t: 'El Decreto del Viejo Horno', s: 'Don Lupe lanza el ultimátum: quien cree el pan de plátano definitivo y lo lleve a tiempo al Festival será Empleado del Mes.' },
    { k: 'Acto I', t: 'La Cueva del Mercado y el Fruto Prohibido', s: 'Escalada vertical por el Mercado Municipal para arrancar los plátanos machos reales a Polifemo, mientras Circe arroja cáscaras.' },
    { k: 'Acto II', t: 'El Asedio a la Panadería de Piedra', s: 'Ratas, jejenes, cocodrilos y ayudantes de Circe asedian el horno de barro mientras el pan sube.' },
    { k: 'Acto III', t: 'Carrera Desesperada por los Manglares de La Tovara', s: 'Con la canasta a la espalda, Ulises cruza el manglar a contrarreloj hasta la carpa de los jueces.' },
  ],
  personajes: [
    { id: 'ulises', name: 'Ulises', role: 'El Panadero', tone: 'ally', tag: 'Héroe', img: '../../assets/characters/ulises-sheet.png', d: 'Joven artesano de San Blas con un talento nato para las masas y una terquedad inquebrantable. Decidido a honrar la receta tradicional de su pueblo, no descansará hasta demostrar que el verdadero sabor nace del corazón y la leña.' },
    { id: 'lupe', name: 'Don Lupe', role: 'El Maestro Panadero', tone: 'ally', tag: 'Aliado', img: '../../assets/characters/don-lupe-sheet.png', d: 'Veterano dueño de la panadería tradicional y guardián del horno de barro más antiguo del puerto. Cansado pero sabio, busca a un sucesor digno que mantenga viva la esencia culinaria de la costa.' },
    { id: 'circe', name: 'Madame Circe', role: 'La Chef Rival', tone: 'rival', tag: 'Rival', img: '../../assets/characters/madame-circe-sheet.png', d: 'Cocinera corporativa y pretenciosa que busca conquistar la Riviera Nayarit usando atajos químicos y maquinaria importada. Recurre al sabotaje constante para aplastar el talento local.' },
    { id: 'polifemo', name: 'Polifemo', role: 'El Estibador del Mercado', tone: 'neutral', tag: 'Guardián', img: '../../assets/characters/polifemo-sheet.png', d: 'Gigantesco y malhumorado comerciante que vigila la cima de los huacales. Parece intratable, pero su debilidad por las distracciones lo hace vulnerable al ingenio.' },
  ],
  receta: [
    { i: 'nutrition', n: 'Plátano macho real', w: 'Nivel 1 · cima del mercado' },
    { i: 'grain', n: 'Harina de trigo', w: 'Nivel 1' },
    { i: 'egg', n: 'Huevos', w: 'Nivel 1' },
    { i: 'cookie', n: 'Azúcar pura de caña', w: 'Nivel 1' },
    { i: 'spa', n: 'Canela en rama', w: 'Morral de especias' },
    { i: 'forest', n: 'Leña de horno de barro', w: 'Nivel 2' },
  ],
};

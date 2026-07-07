       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLIENTES.

       DATA DIVISION.
       WORKING-STORAGE SECTION.

       01 WS-ACAO              PIC X(10).
       01 WS-CODIGO            PIC X(5).
       01 WS-NOME              PIC X(50).
       01 WS-TELEFONE          PIC X(15).
       01 WS-EMAIL             PIC X(50).
       01 WS-NOVO-TELEFONE     PIC X(15).
       01 WS-NOVO-EMAIL        PIC X(50).
       01 WS-INPUT-LINE        PIC X(200).

       PROCEDURE DIVISION.
       MAIN-PARA.
           ACCEPT WS-INPUT-LINE FROM CONSOLE.

           UNSTRING WS-INPUT-LINE DELIMITED BY '|'
               INTO WS-ACAO
                    WS-CODIGO
                    WS-NOME
                    WS-TELEFONE
                    WS-EMAIL
                    WS-NOVO-TELEFONE
                    WS-NOVO-EMAIL
           END-UNSTRING.

           EVALUATE TRUE
               WHEN WS-ACAO = "CONSULTAR  "
                   PERFORM CONSULTAR-CLIENTE
               WHEN WS-ACAO = "ATUALIZAR  "
                   PERFORM ATUALIZAR-CLIENTE
               WHEN OTHER
                   DISPLAY "ERRO|ACAO_INVALIDA"
           END-EVALUATE.

           STOP RUN.

      *------------------------------------
       CONSULTAR-CLIENTE.
           IF WS-NOME = SPACES
               DISPLAY "NOTFOUND"
           ELSE
               DISPLAY "OK|" FUNCTION TRIM(WS-CODIGO)
                       "|" FUNCTION TRIM(WS-NOME)
                       "|" FUNCTION TRIM(WS-TELEFONE)
                       "|" FUNCTION TRIM(WS-EMAIL)
           END-IF.

      *------------------------------------
       ATUALIZAR-CLIENTE.
           IF WS-NOME = SPACES
               DISPLAY "NOTFOUND"
           ELSE
               DISPLAY "ATUALIZADO|" FUNCTION TRIM(WS-CODIGO)
                       "|" FUNCTION TRIM(WS-NOVO-TELEFONE)
                       "|" FUNCTION TRIM(WS-NOVO-EMAIL)
           END-IF.

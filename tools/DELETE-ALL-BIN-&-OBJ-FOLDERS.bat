@echo OFF

for /D %%G in (SMATK*) do ( 
	if exist %%G\bin (
		rd /s /q %%G\bin
	)
	if exist %%G\obj (
		rd /s /q %%G\obj
	)
)

@ECHO ON